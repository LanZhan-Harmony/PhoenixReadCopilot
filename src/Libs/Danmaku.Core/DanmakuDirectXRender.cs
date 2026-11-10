using System.Diagnostics;
using Danmaku.Core.Enums;
using Danmaku.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SharpGen.Runtime;
using Vortice.DirectWrite;
using Vortice.DXGI;

namespace Danmaku.Core;

internal sealed partial class DanmakuDirectXRender : IDisposable, IGameLoopClient
{
    internal DanmakuDirectXRender(
        SwapChainPanel swapChainPanel,
        FrameworkElement container,
        DanmakuConfiguration initialConfig = null
    )
    {
        _swapChainPanel = swapChainPanel ?? throw new ArgumentNullException(nameof(swapChainPanel));
        _scale = (float)(swapChainPanel.XamlRoot?.RasterizationScale ?? 1.0);
        _dpi = 96.0f * _scale; // 超采样 DPI

        // 逻辑像素尺寸
        CanvasWidth = (float)_swapChainPanel.ActualWidth * _scale;
        CanvasHeight = (float)_swapChainPanel.ActualHeight * _scale;

        // 初始化配置状态（使用传入的配置或默认配置）
        DanmakuConfiguration config = initialConfig ?? DanmakuConfiguration.CreateDefault();
        _configState = DanmakuConfigurationState.FromConfiguration(config);
        // 确保滚动速度使用正确的 scale
        if (config.RollingSpeedBase.HasValue)
        {
            _configState.RollingSpeed = (float)(config.RollingSpeedBase.Value * 0.02f * _scale);
        }

        Logger.Log(
            $"Initialize: LogicalSize={CanvasWidth}x{CanvasHeight}, Scale={_scale}, DPI={_dpi}"
        );

        // 🚀 优化 #5：纹理缓存 + 自动清理
        // 策略：弹幕在屏幕上时使用缓存，消失后立即释放
        // 优点：保留性能优势，同时避免内存累积
        _enableTextureCache = true;

        // 初始化 DirectX 资源管理器
        _resourceManager = new DirectXResourceManager(_swapChainPanel, _scale);

        // 订阅资源管理器事件
        _resourceManager.ScaleChanged += OnResourceManagerScaleChanged;
        _resourceManager.SizeChanged += OnResourceManagerSizeChanged;

        uint layerCount = DanmakuDefaultLayerDef.DefaultLayerCount;
        _renderLayerList = new RenderLayer[layerCount];
        for (uint i = 0; i < layerCount; i++)
        {
            _renderLayerList[i] = new RenderLayer(
                i,
                i == DanmakuDefaultLayerDef.AdvancedLayerId
                    || i == DanmakuDefaultLayerDef.SubtitleLayerId
            );
            if (CanvasHeight >= 1f)
            {
                _renderLayerList[i]
                    .UpdateYSlotManagerLength((uint)CanvasHeight, _configState.RollingAreaRatio);
            }
        }

        // 🚀 GC 优化：预分配快照数组，避免每帧 ToArray()
        _renderSnapshots = new DanmakuDirectXRenderItem[layerCount][];
        for (int i = 0; i < layerCount; i++)
        {
            _renderSnapshots[i] = new DanmakuDirectXRenderItem[128]; // 初始容量
        }

        // 🚀 混合渲染架构：
        // 策略：CompositionTarget.Rendering 提供渲染时机，GameLoopThread 执行实际渲染
        // 优点：
        // - 渲染时机与系统完美同步（避免卡顿）
        // - 渲染在专用线程执行（避免 UI 线程阻塞）
        // - 不需要手动 vsync 等待
        _frameStopwatch.Start();

        // 创建游戏循环线程（用于渲染）
        _gameLoopThread = new GameLoopThread(_swapChainPanel, this);

        // 订阅 CompositionTarget.Rendering 作为渲染时机信号
        CompositionTarget.Rendering += OnRenderingSignal;

        Logger.Log("DanmakuDirectXRender is created (Hybrid Rendering Mode)");
    }

    /// <summary>
    /// 更新弹幕配置.
    /// </summary>
    /// <param name="config">新的配置（只有非空值会被应用）.</param>
    public void UpdateConfiguration(DanmakuConfiguration config)
    {
        if (config == null)
        {
            return;
        }

        // 应用配置，传入回调用于更新 Y 轴槽管理器
        _configState.ApplyConfiguration(
            config,
            _scale,
            rollingAreaRatio =>
            {
                // 当滚动区域比例变化时，更新所有层的 Y 轴槽管理器
                if (_renderLayerList != null)
                {
                    for (int i = 0; i < _renderLayerList.Length; i++)
                    {
                        _renderLayerList[i]
                            .UpdateYSlotManagerLength((uint)CanvasHeight, rollingAreaRatio);
                    }
                }
            }
        );

        Logger.Log($"Configuration updated");
    }

    /// <summary>
    /// CompositionTarget.Rendering 事件处理器（仅作为时机信号，不执行渲染）.
    /// 这个方法在 UI 线程上调用，非常快速，立即将渲染任务委托给 GameLoopThread.
    /// </summary>
    private void OnRenderingSignal(object? sender, object e)
    {
        // 快速检查：避免重复调度
        if (_isPaused || _isStopped || _isRendering)
        {
            return;
        }

        // 立即标记渲染状态，防止高刷屏下同一时钟周期内重复排队
        _isRendering = true;

        // 将实际渲染工作委托给 GameLoopThread
        // 这样既能利用 CompositionTarget.Rendering 的最佳时机，又不阻塞 UI 线程
        var task = _gameLoopThread?.RunAsync(RenderFrame);
        if (task == null)
        {
            _isRendering = false;
        }
    }

    private void OnResourceManagerScaleChanged(object? sender, float scale)
    {
        _scale = scale;
        _dpi = 96.0f * _scale;
        CanvasWidth = _resourceManager.CanvasWidth;
        CanvasHeight = _resourceManager.CanvasHeight;

        Logger.Log($"Scale changed: {_scale}, DPI={_dpi}, Canvas={CanvasWidth}x{CanvasHeight}");
    }

    private void OnResourceManagerSizeChanged(object? sender, (double Width, double Height) size)
    {
        if (_renderLayerList is null)
        {
            return;
        }

        CanvasWidth = _resourceManager.CanvasWidth;
        CanvasHeight = _resourceManager.CanvasHeight;

        for (int i = 0; i < _renderLayerList.Length; i++)
        {
            _renderLayerList[i]
                .UpdateYSlotManagerLength((uint)CanvasHeight, _configState.RollingAreaRatio);
        }

        Logger.Log($"Size changed: Canvas={CanvasWidth}x{CanvasHeight}");
    }

    public void RenderDanmakuItem(uint layerId, DanmakuItem danmakuItem)
    {
        if (_resourceManager.D2DContext == null || _isStopped || CanvasWidth <= 0)
        {
            return;
        }

        if (layerId >= _renderLayerList.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(layerId),
                $"Max layer count: {_renderLayerList.Length}"
            );
        }

        DanmakuYSlotManager ySlotManager = _renderLayerList[layerId].YSlotManager;

        // 🚀 对象池优化：从对象池租用 RenderItem
        DanmakuDirectXRenderItem? renderItem = null;
        IDWriteTextLayout? textLayout = null;
        bool addedToRenderList = false;

        try
        {
            renderItem = _resourceManager.ObjectPool.RentRenderItem(danmakuItem);

            if (renderItem.Item.Mode == DanmakuMode.Unknown)
            {
                Logger.Log(
                    $"Ignore unknown danmaku type ({renderItem.Item.Mode}): {renderItem.Item.Text}"
                );
                return;
            }

            if (
                !_configState.AutoControlDensity
                && _configState.RollingDensity > 0
                && renderItem.Item.Mode == DanmakuMode.Rolling
                && _renderLayerList[layerId].RenderList.Count >= _configState.RollingDensity
            )
            {
                return;
            }

            // Create text format - 使用缓存
            bool isBold = renderItem.Item.IsBold ?? _configState.IsTextBold;
            // Adjust font size
            float fontSize = (float)(renderItem.FontSize * _scale * _configState.FontSizeScale);

            string fontFamily = string.IsNullOrWhiteSpace(renderItem.Item.FontFamilyName)
                ? _configState.FontFamilyName
                : renderItem.Item.FontFamilyName;
            IDWriteTextFormat? textFormat = GetOrCreateTextFormat(fontFamily, fontSize, isBold);

            // Create text layout
            textLayout = _resourceManager.DWriteFactory.CreateTextLayout(
                renderItem.Item.Text,
                textFormat,
                (CanvasWidth) - 24f,
                CanvasHeight
            );

            TextMetrics textMetrics = textLayout.Metrics;
            renderItem.Width = textMetrics.Width + 8f;
            renderItem.Height = textMetrics.Height;

            if (renderItem.Item.HasOutline)
            {
                renderItem.Height += renderItem.Item.OutlineSize;
            }

            if (renderItem.Width <= 0 || renderItem.Height <= 0)
            {
                return;
            }

            if (
                _maxDanmakuSize > 0
                && (renderItem.Width >= _maxDanmakuSize || renderItem.Height >= _maxDanmakuSize)
            )
            {
                return;
            }

            renderItem.TextLayout = textLayout;
            renderItem.TextFormat = textFormat!;
            textLayout = null; // 所有权转移到 renderItem

            // Calculate initial position
            switch (renderItem.Item.Mode)
            {
                case DanmakuMode.Rolling:
                    renderItem.NeedToReleaseYSlot = ySlotManager.GetY(
                        renderItem.Id,
                        (uint)renderItem.Height,
                        out uint yRolling
                    );
                    renderItem.StartX = CanvasWidth;
                    renderItem.StartY = yRolling;
                    break;
                case DanmakuMode.Bottom:
                    renderItem.NeedToReleaseYSlot = ySlotManager.GetY(
                        renderItem.Id,
                        (uint)renderItem.Height,
                        out uint yBottom
                    );
                    renderItem.StartY = yBottom;
                    break;
                case DanmakuMode.Top:
                    renderItem.NeedToReleaseYSlot = ySlotManager.GetY(
                        renderItem.Id,
                        (uint)renderItem.Height,
                        out uint yTop
                    );
                    renderItem.StartY = yTop;
                    break;
                case DanmakuMode.ReverseRolling:
                    renderItem.NeedToReleaseYSlot = ySlotManager.GetY(
                        renderItem.Id,
                        (uint)renderItem.Height,
                        out uint yReverse
                    );
                    renderItem.StartX = -renderItem.Width;
                    renderItem.StartY = yReverse;
                    break;
                case DanmakuMode.Advanced:
                    CalculateAdvancedPosition(renderItem);
                    break;
            }

            renderItem.X = renderItem.StartX;

            if (
                _configState.AutoControlDensity
                && renderItem.Item.AllowDensityControl
                && !renderItem.NeedToReleaseYSlot
            )
            {
                return;
            }

            lock (_renderLayerList[layerId].RenderList)
            {
                _renderLayerList[layerId].RenderList.Add(renderItem);
                addedToRenderList = true; // 标记已添加
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error rendering danmaku: {ex.Message}");
        }
        finally
        {
            // 🔧 确保资源清理
            // 如果 textLayout 还没有转移给 renderItem，需要 Dispose
            textLayout?.Dispose();

            // 如果 renderItem 没有添加到渲染列表，需要归还
            if (renderItem != null && !addedToRenderList)
            {
                _resourceManager.ObjectPool.ReturnRenderItem(renderItem);
            }
        }
    }

    private void CalculateAdvancedPosition(DanmakuDirectXRenderItem renderItem)
    {
        // Similar logic to original implementation
        if (renderItem.Item.AlignmentMode == DanmakuAlignmentMode.Default)
        {
            renderItem.StartX =
                renderItem.Item.StartX > 1.0f
                    ? renderItem.Item.StartX
                    : renderItem.Item.StartX * CanvasWidth;
            renderItem.StartY =
                renderItem.Item.StartY > 1.0f
                    ? renderItem.Item.StartY
                    : renderItem.Item.StartY * CanvasHeight;
            renderItem.EndX =
                renderItem.Item.EndX > 1.0f
                    ? renderItem.Item.EndX
                    : renderItem.Item.EndX * CanvasWidth;
            renderItem.EndY =
                renderItem.Item.EndY > 1.0f
                    ? renderItem.Item.EndY
                    : renderItem.Item.EndY * CanvasHeight;

            // Apply anchor mode adjustments
            switch (renderItem.Item.AnchorMode)
            {
                case DanmakuAlignmentMode.LowerCenter:
                case DanmakuAlignmentMode.MiddleCenter:
                case DanmakuAlignmentMode.UpperCenter:
                    renderItem.StartX -= renderItem.Width / 2;
                    renderItem.EndX -= renderItem.Width / 2;
                    break;
                case DanmakuAlignmentMode.LowerRight:
                case DanmakuAlignmentMode.MiddleRight:
                case DanmakuAlignmentMode.UpperRight:
                    renderItem.StartX -= renderItem.Width;
                    renderItem.EndX -= renderItem.Width;
                    break;
            }

            switch (renderItem.Item.AnchorMode)
            {
                case DanmakuAlignmentMode.LowerLeft:
                case DanmakuAlignmentMode.LowerCenter:
                case DanmakuAlignmentMode.LowerRight:
                    renderItem.StartY -= renderItem.Height;
                    renderItem.EndY -= renderItem.Height;
                    break;
                case DanmakuAlignmentMode.MiddleLeft:
                case DanmakuAlignmentMode.MiddleCenter:
                case DanmakuAlignmentMode.MiddleRight:
                    renderItem.StartY -= renderItem.Height / 2;
                    renderItem.EndY -= renderItem.Height / 2;
                    break;
            }
        }
        else
        {
            // Alignment mode based positioning
            if (
                renderItem.Item.AlignmentMode == DanmakuAlignmentMode.LowerLeft
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.MiddleLeft
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.UpperLeft
            )
            {
                renderItem.StartX = renderItem.Item.MarginLeft;
            }
            else if (
                renderItem.Item.AlignmentMode == DanmakuAlignmentMode.LowerCenter
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.MiddleCenter
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.UpperCenter
            )
            {
                renderItem.StartX = (CanvasWidth - renderItem.Width) / 2;
            }
            else if (
                renderItem.Item.AlignmentMode == DanmakuAlignmentMode.LowerRight
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.MiddleRight
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.UpperRight
            )
            {
                renderItem.StartX = CanvasWidth - renderItem.Width - renderItem.Item.MarginRight;
            }

            if (
                renderItem.Item.AlignmentMode == DanmakuAlignmentMode.LowerLeft
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.LowerCenter
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.LowerRight
            )
            {
                renderItem.StartY = CanvasHeight - renderItem.Height - renderItem.Item.MarginBottom;
            }
            else if (
                renderItem.Item.AlignmentMode == DanmakuAlignmentMode.MiddleLeft
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.MiddleCenter
                || renderItem.Item.AlignmentMode == DanmakuAlignmentMode.MiddleRight
            )
            {
                renderItem.StartY = (CanvasHeight - renderItem.Height) / 2;
            }
            else
            {
                renderItem.StartY = 0;
            }

            renderItem.EndX = renderItem.StartX;
            renderItem.EndY = renderItem.StartY;
        }

        renderItem.TranslationSpeedX =
            renderItem.Item.TranslationDurationMs > 0
                ? (renderItem.EndX - renderItem.StartX) / renderItem.Item.TranslationDurationMs
                : 0;
        renderItem.TranslationSpeedY =
            renderItem.Item.TranslationDurationMs > 0
                ? (renderItem.EndY - renderItem.StartY) / renderItem.Item.TranslationDurationMs
                : 0;
    }

    private void UpdateDanmaku(TimeSpan elapsedTime, TimeSpan totalTime)
    {
        float elapsedMs = (float)elapsedTime.TotalMilliseconds;
        UpdatePerformanceStats(elapsedTime);

        // 如果启用并行更新且弹幕数量较多，使用并行处理
        if (_configState.EnableParallelUpdate)
        {
            // 并行处理每一层
            System.Threading.Tasks.Parallel.For(
                0,
                _renderLayerList.Length,
                layerId => UpdateLayerDanmaku((uint)layerId, elapsedMs, totalTime)
            );
        }
        else
        {
            // 顺序处理（默认）
            for (uint layerId = 0; layerId < _renderLayerList.Length; layerId++)
            {
                UpdateLayerDanmaku(layerId, elapsedMs, totalTime);
            }
        }
    }

    /// <summary>
    /// 更新单个层的弹幕（线程安全）
    /// </summary>
    private void UpdateLayerDanmaku(uint layerId, float elapsedMs, TimeSpan totalTime)
    {
        // 关键修复：在更新前检查停止标志，避免在 UI 事件期间继续处理
        if (_isStopped)
        {
            return;
        }

        DanmakuYSlotManager ySlotManager = _renderLayerList[layerId].YSlotManager;
        List<DanmakuDirectXRenderItem> renderList = _renderLayerList[layerId].RenderList;

        lock (renderList)
        {
            for (int i = renderList.Count - 1; i >= 0; i--)
            {
                // 每次迭代都检查停止标志
                if (_isStopped)
                {
                    return;
                }

                DanmakuDirectXRenderItem renderItem = renderList[i];
                if (!renderItem.IsFirstRenderTimeSet)
                {
                    renderItem.FirstRenderTime = totalTime;
                    renderItem.IsFirstRenderTimeSet = true;
                }

                float durationMs = (float)
                    (totalTime - renderItem.FirstRenderTime).TotalMilliseconds;
                bool removeItem = false;

                switch (renderItem.Item.Mode)
                {
                    case DanmakuMode.Rolling:
                        if (!_isPaused)
                        {
                            renderItem.X -=
                                elapsedMs
                                * AdjustRollingSpeedByWidth(
                                    _configState.RollingSpeed,
                                    renderItem.Width
                                );
                        }
                        if (
                            renderItem.NeedToReleaseYSlot
                            && renderItem.X < CanvasWidth - renderItem.Width - 48
                        )
                        {
                            ySlotManager.ReleaseYSlot(renderItem.Id, (uint)renderItem.StartY);
                            renderItem.NeedToReleaseYSlot = false;
                        }
                        if (renderItem.X < (-renderItem.Width * 1.2))
                        {
                            removeItem = true;
                        }
                        break;

                    case DanmakuMode.Bottom:
                    case DanmakuMode.Top:
                        renderItem.X = (CanvasWidth - renderItem.Width) / 2;
                        float maxDurationMs =
                            renderItem.Item.DurationMs > 0
                                ? renderItem.Item.DurationMs
                                : DefaultBottomAndTopDurationMs;
                        if (durationMs > maxDurationMs)
                        {
                            removeItem = true;
                            if (renderItem.NeedToReleaseYSlot)
                            {
                                ySlotManager.ReleaseYSlot(renderItem.Id, (uint)renderItem.StartY);
                            }
                        }
                        break;

                    case DanmakuMode.ReverseRolling:
                        if (!_isPaused)
                        {
                            renderItem.X +=
                                elapsedMs
                                * AdjustRollingSpeedByWidth(
                                    _configState.RollingSpeed,
                                    renderItem.Width
                                );
                        }
                        if (renderItem.NeedToReleaseYSlot && renderItem.X > 48f)
                        {
                            ySlotManager.ReleaseYSlot(renderItem.Id, (uint)renderItem.StartY);
                            renderItem.NeedToReleaseYSlot = false;
                        }
                        if (renderItem.X >= CanvasWidth)
                        {
                            removeItem = true;
                        }
                        break;
                }

                if (removeItem)
                {
                    // 🚀 对象池优化：归还 RenderItem 到对象池
                    TextureCacheKey key = GetTextureCacheKey(renderItem);
                    _resourceManager.ObjectPool.MarkTextureUnused(key);
                    _resourceManager.ObjectPool.ReturnRenderItem(renderItem);

                    renderList.RemoveAt(i);
                }
            }
        }
    }

    private void RenderDanmaku()
    {
        // 关键修复：在渲染前再次检查状态，防止在 UI 事件处理期间访问资源
        if (
            _resourceManager.D2DContext == null
            || CanvasWidth <= 0
            || CanvasHeight <= 0
            || _isStopped
        )
        {
            return;
        }

        try
        {
            _resourceManager.D2DContext.BeginDraw();
            _resourceManager.D2DContext.Clear(_clearColor); // 🚀 优化：使用预分配的 Color4

            int totalCount = 0;

            for (uint layerId = 0; layerId < _renderLayerList.Length; layerId++)
            {
                if (!_renderLayerList[layerId].IsEnabled)
                {
                    continue;
                }

                List<DanmakuDirectXRenderItem> renderList = _renderLayerList[layerId].RenderList;
                if (renderList.Count == 0)
                {
                    continue;
                }

                // 🚀 GC 优化：使用预分配数组替代 ToArray()，避免每帧分配
                int count;
                lock (renderList)
                {
                    count = renderList.Count;

                    // 如果预分配数组不够大，扩容（2倍增长）
                    if (_renderSnapshots[layerId].Length < count)
                    {
                        _renderSnapshots[layerId] = new DanmakuDirectXRenderItem[count * 2];
                    }

                    // 快速复制到预分配数组（CopyTo 比 ToArray 快且无 GC）
                    renderList.CopyTo(_renderSnapshots[layerId], 0);
                }

                // 🎯 无锁渲染快照（不阻塞更新线程）
                for (int i = 0; i < count; i++)
                {
                    DanmakuDirectXRenderItem renderItem = _renderSnapshots[layerId][i];

                    if (_isStopped || !renderItem.IsFirstRenderTimeSet)
                    {
                        continue;
                    }

                    totalCount++;

                    float x = renderItem.X;
                    float y;

                    switch (renderItem.Item.Mode)
                    {
                        case DanmakuMode.Rolling:
                        case DanmakuMode.ReverseRolling:
                            y = renderItem.StartY;
                            break;

                        case DanmakuMode.Bottom:
                            y = Math.Max(
                                (
                                    _configState.NoOverlapSubtitle
                                        ? Math.Max(CanvasHeight - 100f, CanvasHeight * 0.8f)
                                        : CanvasHeight
                                )
                                    - renderItem.Height
                                    - renderItem.StartY,
                                0
                            );
                            y -= renderItem.Item.MarginBottom;
                            break;

                        case DanmakuMode.Top:
                            y = renderItem.StartY;
                            break;

                        default:
                            y = 0;
                            break;
                    }

                    // Draw danmaku
                    DrawDanmakuText(renderItem, x, y);
                }
            }

            // Debug info (仅在需要时)
            if (DebugMode)
            {
                RenderDebugInfo(totalCount);
            }

            Result result = _resourceManager.D2DContext.EndDraw(out _, out _);
            if (result.Failure)
            {
                Logger.Log($"EndDraw failed: {result}");
                // Device might be lost, try to recover on next frame
                if (
                    _resourceManager.D3DDevice != null
                    && _resourceManager.D3DDevice.DeviceRemovedReason.Failure
                )
                {
                    Logger.Log($"Device lost: {_resourceManager.D3DDevice.DeviceRemovedReason}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"RenderDanmaku error: {ex.Message}");
        }
    }

    public void SetLayerRenderState(uint layerId, bool render)
    {
        if (_renderLayerList != null && layerId < _renderLayerList.Length)
        {
            _renderLayerList[layerId].IsEnabled = render;
        }
    }

    public void ClearLayer(uint layerId)
    {
        if (_renderLayerList != null && layerId < _renderLayerList.Length)
        {
            _renderLayerList[layerId].Clear(_resourceManager.ObjectPool);
        }
    }

    public void Clear()
    {
        if (_renderLayerList == null)
        {
            return;
        }

        for (int i = 0; i < _renderLayerList.Length; i++)
        {
            _renderLayerList[i].Clear(_resourceManager.ObjectPool);
        }

        Debug.WriteLine("DanmakuDirectXRender is cleared");
    }

    public void Start()
    {
        _isStopped = false;
        _isPaused = false;
        _frameStopwatch.Start();

        // 启动游戏循环调度器（用于渲染）
        _gameLoopThread?.StartDispatcher();

        Debug.WriteLine("DanmakuDirectXRender is started (Hybrid Rendering Mode)");
    }

    public void Pause()
    {
        _isPaused = true;
        _frameStopwatch.Stop();
        Debug.WriteLine("DanmakuDirectXRender is paused");
    }

    public void Stop()
    {
        _isStopped = true;

        // 停止游戏循环调度器
        _gameLoopThread?.StopDispatcher();

        Clear();
        Logger.Log("DanmakuDirectXRender is stopped");
    }

    public void Dispose()
    {
        // 关键修复：先停止渲染循环，避免在 Dispose 期间继续访问资源
        // 1. 立即停止渲染（设置标志位）
        _isStopped = true;
        _isPaused = true;

        // 2. 取消订阅 CompositionTarget.Rendering
        CompositionTarget.Rendering -= OnRenderingSignal;

        // 3. 停止游戏循环线程
        _gameLoopThread?.Dispose();
        _gameLoopThread = null;

        // 4. 等待当前帧完成（给一点时间让正在执行的 OnRendering 完成）
        // 注意：不能用 Thread.Sleep，因为可能在 UI 线程
        // 使用标志位 _isStopped 让 OnRendering 自己提前退出

        // 5. 清理弹幕列表
        Clear();

        // 6. 清理事件订阅
        if (_resourceManager != null)
        {
            _resourceManager.ScaleChanged -= OnResourceManagerScaleChanged;
            _resourceManager.SizeChanged -= OnResourceManagerSizeChanged;
        }

        // 7. 清理 SwapChainPanel 引用（避免循环引用）
        if (_swapChainPanel != null)
        {
            _swapChainPanel = null;
        }

        // 8. 清理缓存的描边画刷
        _cachedOutlineBrush?.Dispose();
        _cachedOutlineBrush = null;

        // 9. 清理调试渲染器
        _debugRenderer?.Dispose();
        _debugRenderer = null;

        // 10. 最后释放资源管理器（包含 DirectX 资源）
        _resourceManager?.Dispose();

        Logger.Log("DanmakuDirectXRender is disposed");
    }

    private void RenderDebugInfo(int totalCount)
    {
        if (CanvasWidth == 0 || _resourceManager == null)
        {
            return;
        }

        _debugRenderer ??= new DebugRenderer(
            _resourceManager.D2DContext,
            _resourceManager.DWriteFactory
        );
        DebugInfo debugInfo = new()
        {
            BufferWidth = (int)CanvasWidth,
            BufferHeight = (int)CanvasHeight,
            LogicalWidth = _resourceManager.LogicalWidth,
            LogicalHeight = _resourceManager.LogicalHeight,
            ScaleX = _scale,
            ScaleY = _scale,
            DanmakuCount = totalCount,
            RunningTime = _lastFrameTime, // 这里可以从外部传入总时间
            FrameRate = _currentFrameRate,
        };

        _debugRenderer.RenderDebugInfo(debugInfo, _scale);
    }

    /// <summary>
    /// 更新性能统计
    /// </summary>
    private void UpdatePerformanceStats(TimeSpan elapsedTime)
    {
        // 使用传入的精确帧间隔时间计算帧率
        if (elapsedTime.TotalSeconds > 0)
        {
            // 计算当前帧率（使用指数移动平均平滑处理）
            var currentFps = 1.0 / elapsedTime.TotalSeconds;
            _currentFrameRate = _currentFrameRate * 0.9 + currentFps * 0.1;
        }
    }

    #region IGameLoopClient 实现

    /// <summary>
    /// 游戏循环启动时调用.
    /// </summary>
    void IGameLoopClient.OnGameLoopStarting()
    {
        Logger.Log("Game loop thread started");
    }

    /// <summary>
    /// 游戏循环停止时调用.
    /// </summary>
    void IGameLoopClient.OnGameLoopStopped()
    {
        Logger.Log("Game loop thread stopped");
    }

    #endregion

    #region 混合渲染模式实现

    /// <summary>
    /// 渲染帧的主方法（在游戏循环线程上调用）.
    /// </summary>
    private void RenderFrame()
    {
        if (_isPaused || _isStopped || _resourceManager.D2DContext == null)
        {
            _isRendering = false;
            return;
        }

        try
        {
            lock (_resourceManager.RenderLock)
            {
                if (_isPaused || _isStopped || _resourceManager.D2DContext == null)
                {
                    return;
                }

                TimeSpan currentTime = _frameStopwatch.Elapsed;
                TimeSpan elapsedTime = currentTime - _lastFrameTime;
                _lastFrameTime = currentTime;

                // 限制单帧最大时间跨度（防止系统挂起或掉帧时弹幕剧烈瞬移）
                if (elapsedTime.TotalMilliseconds > 100.0)
                {
                    elapsedTime = TimeSpan.FromMilliseconds(100.0);
                }

                _totalTime += elapsedTime;

                // 更新性能统计
                UpdatePerformanceStats(elapsedTime);

                // 更新弹幕位置
                UpdateDanmaku(elapsedTime, _totalTime);

                // 渲染弹幕
                RenderDanmaku();

                // 🚀 核心优化：使用 Present(0) 立即提交后备缓冲到 DWM，不等待垂直消隐阻塞
                // 解决外层 CompositionTarget.Rendering 与底层 Present(1) 产生的双重 V-Sync 等待导致帧率腰斩卡顿的问题
                Result? presentResult = _resourceManager.SwapChain?.Present(0, PresentFlags.None);
                if (presentResult.HasValue && presentResult.Value.Failure)
                {
                    Logger.Log($"Present failed: {presentResult.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Render error: {ex.Message}");
        }
        finally
        {
            _isRendering = false;
        }

        // CompositionTarget.Rendering 会自动触发下一帧
    }

    #endregion
}
