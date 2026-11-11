using System.Diagnostics;
using Danmaku.Core.Enums;
using Danmaku.Core.Models;
using Microsoft.UI.Xaml.Controls;
using SharpGen.Runtime;
using Vortice.DirectWrite;
using Vortice.DXGI;

namespace Danmaku.Core;

internal sealed partial class DanmakuDirectXRender : IDisposable
{
    internal DanmakuDirectXRender(
        SwapChainPanel swapChainPanel,
        DanmakuConfiguration? initialConfig = null
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

        // 初始化红果表情管理器
        _emojiManager = new DanmakuEmojiManager(_resourceManager);

        uint layerCount = DanmakuDefaultLayerDef.DefaultLayerCount;
        _renderLayerList = new RenderLayer[layerCount];
        for (uint i = 0; i < layerCount; i++)
        {
            _renderLayerList[i] = new RenderLayer(i, i == DanmakuDefaultLayerDef.SubtitleLayerId);
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

        // 🚀 独立自主渲染架构：
        // 策略：GameLoopThread 在高优先级独立工作线程自主循环，以 DXGI 硬件垂直同步 (VSync) 直接驱动，完全脱离 UI 线程
        // 优点：
        // - 100% 跑满显示器物理刷新率（60Hz/120Hz/144Hz/240Hz）
        // - 彻底消除 WinUI 3 UI 消息泵抖动与跨线程调度丢帧
        // - 低通滤波平滑步长，消除弹幕位移微抖动
        _frameStopwatch.Start();

        // 创建独立渲染循环线程
        _gameLoopThread = new GameLoopThread(RenderFrame);

        Logger.Log("DanmakuDirectXRender is created (Independent Game Loop Mode)");
    }

    /// <summary>
    /// 更新弹幕配置.
    /// </summary>
    /// <param name="config">新的配置（只有非空值会被应用）.</param>
    public void UpdateConfiguration(DanmakuConfiguration config)
    {
        if (config is null)
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
                if (_renderLayerList is not null)
                {
                    foreach (RenderLayer layer in _renderLayerList)
                    {
                        layer.UpdateYSlotManagerLength((uint)CanvasHeight, rollingAreaRatio);
                    }
                }
            }
        );

        Logger.Log($"Configuration updated");
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

        foreach (RenderLayer layer in _renderLayerList)
        {
            layer.UpdateYSlotManagerLength((uint)CanvasHeight, _configState.RollingAreaRatio);
        }

        // 尺寸变化后，重置上一帧时间戳，并唤醒独立渲染线程重绘
        _lastFrameTime = _frameStopwatch.Elapsed;
        _smoothedElapsedMs = 0f;
        _hasClearedScreen = false;
        _gameLoopThread?.Wake();

        Logger.Log($"Size changed: Canvas={CanvasWidth}x{CanvasHeight}");
    }

    public void RenderDanmakuItem(uint layerId, DanmakuItem danmakuItem)
    {
        if (_resourceManager.D2DContext is null || _isStopped || CanvasWidth <= 0)
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
                _configState is
                {
                    AutoControlDensity: false,
                    RollingDensity: > 0
                }
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

            // 🎨 红果表情解析：如果包含 [表情名]，拆分为混合图文分段
            if (DanmakuEmojiManager.HasEmojiPattern(renderItem.Item.Text))
            {
                List<DanmakuSegment>? segments = _emojiManager.ParseSegments(
                    renderItem.Item.Text,
                    textFormat,
                    fontSize,
                    CanvasWidth - 24f,
                    CanvasHeight
                );

                if (segments is not null && segments.Count > 0)
                {
                    renderItem.Segments = segments;
                    float totalW = 0f;
                    float maxH = 0f;
                    foreach (DanmakuSegment segment in segments)
                    {
                        totalW += segment.Width;
                        if (segment.Height > maxH)
                        {
                            maxH = segment.Height;
                        }
                    }
                    renderItem.Width = totalW + 8f;
                    renderItem.Height = maxH;
                }
            }

            // 🔤 纯文字弹幕：走标准的单个 TextLayout
            if (renderItem.Segments is null)
            {
                textLayout = _resourceManager.DWriteFactory.CreateTextLayout(
                    renderItem.Item.Text,
                    textFormat,
                    CanvasWidth - 24f,
                    CanvasHeight
                );

                TextMetrics textMetrics = textLayout.Metrics;
                renderItem.Width = textMetrics.Width + 8f;
                renderItem.Height = textMetrics.Height;
                renderItem.TextLayout = textLayout;
                textLayout = null; // 所有权转移到 renderItem
            }

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

            renderItem.TextFormat = textFormat!;

            // 计算初始位置
            switch (renderItem.Item.Mode)
            {
                case DanmakuMode.Rolling:
                    renderItem.NeedToReleaseYSlot = ySlotManager.GetY(
                        renderItem.Id,
                        (uint)renderItem.Height,
                        out uint yRolling
                    );
                    renderItem.X = CanvasWidth;
                    renderItem.StartY = yRolling;
                    break;
                case DanmakuMode.Bottom:
                    renderItem.NeedToReleaseYSlot = ySlotManager.GetY(
                        renderItem.Id,
                        (uint)renderItem.Height,
                        out uint yBottom
                    );
                    renderItem.X = (CanvasWidth - renderItem.Width) / 2;
                    renderItem.StartY = yBottom;
                    break;
                case DanmakuMode.Top:
                    renderItem.NeedToReleaseYSlot = ySlotManager.GetY(
                        renderItem.Id,
                        (uint)renderItem.Height,
                        out uint yTop
                    );
                    renderItem.X = (CanvasWidth - renderItem.Width) / 2;
                    renderItem.StartY = yTop;
                    break;
                case DanmakuMode.ReverseRolling:
                    renderItem.NeedToReleaseYSlot = ySlotManager.GetY(
                        renderItem.Id,
                        (uint)renderItem.Height,
                        out uint yReverse
                    );
                    renderItem.X = -renderItem.Width;
                    renderItem.StartY = yReverse;
                    break;
                default:
                    renderItem.X = CanvasWidth;
                    renderItem.StartY = 0;
                    break;
            }

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

            // 唤醒独立渲染线程
            _hasClearedScreen = false;
            _gameLoopThread?.Wake();
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
            if (renderItem is not null && !addedToRenderList)
            {
                _resourceManager.ObjectPool.ReturnRenderItem(renderItem);
            }
        }
    }

    private void UpdateDanmaku(TimeSpan elapsedTime, TimeSpan totalTime)
    {
        float elapsedMs = (float)elapsedTime.TotalMilliseconds;

        // 如果启用并行更新且弹幕数量较多，使用并行处理
        if (_configState.EnableParallelUpdate)
        {
            // 并行处理每一层
            Parallel.For(
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
                        if (renderItem is
                            {
                                NeedToReleaseYSlot: true,
                                X: > 48f
                            })
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
            _resourceManager.D2DContext is null
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
                    float y = renderItem.Item.Mode switch
                    {
                        DanmakuMode.Rolling or DanmakuMode.ReverseRolling => renderItem.StartY,
                        DanmakuMode.Bottom => Math.Max(
                            (
                                _configState.NoOverlapSubtitle
                                    ? Math.Max(CanvasHeight - 100f, CanvasHeight * 0.8f)
                                    : CanvasHeight
                            )
                            - renderItem.Height
                            - renderItem.StartY,
                            0
                        ),
                        DanmakuMode.Top => renderItem.StartY,
                        _ => 0,
                    };

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
                    _resourceManager.D3DDevice is not null
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
        if (_renderLayerList is not null && layerId < _renderLayerList.Length)
        {
            _renderLayerList[layerId].IsEnabled = render;
        }
    }

    public void ClearLayer(uint layerId)
    {
        if (_renderLayerList is not null && layerId < _renderLayerList.Length)
        {
            _renderLayerList[layerId].Clear(_resourceManager.ObjectPool);
        }
    }

    public void Clear()
    {
        if (_renderLayerList is null)
        {
            return;
        }

        foreach (RenderLayer layer in _renderLayerList)
        {
            layer.Clear(_resourceManager.ObjectPool);
        }

        Debug.WriteLine("DanmakuDirectXRender is cleared");
    }

    public void Start()
    {
        _isStopped = false;
        _isPaused = false;
        _lastFrameTime = _frameStopwatch.Elapsed;
        _frameStopwatch.Start();

        if (_gameLoopThread is not null)
        {
            _gameLoopThread.IsPaused = false;
            _gameLoopThread.Wake();
        }

        Logger.Log("DanmakuDirectXRender is started (Independent Game Loop Mode)");
    }

    public void Pause()
    {
        _isPaused = true;
        _gameLoopThread?.IsPaused = true;
        _frameStopwatch.Stop();
        Logger.Log("DanmakuDirectXRender is paused");
    }

    public void Stop()
    {
        _isStopped = true;
        _isPaused = true;

        _gameLoopThread?.IsPaused = true;

        Clear();
        Logger.Log("DanmakuDirectXRender is stopped");
    }

    public void Dispose()
    {
        // 关键修复：先停止渲染循环，避免在 Dispose 期间继续访问资源
        // 1. 立即停止渲染（设置标志位）
        _isStopped = true;
        _isPaused = true;

        // 2. 停止游戏循环线程并等待安全退出
        _gameLoopThread?.Dispose();
        _gameLoopThread = null;

        // 3. 清理弹幕列表
        Clear();

        // 6. 清理事件订阅
        if (_resourceManager is not null)
        {
            _resourceManager.ScaleChanged -= OnResourceManagerScaleChanged;
            _resourceManager.SizeChanged -= OnResourceManagerSizeChanged;
        }

        // 7. 清理 SwapChainPanel 引用（避免循环引用）
        if (_swapChainPanel is not null)
        {
            _swapChainPanel = null;
        }

        // 8. 清理缓存的描边画刷
        _cachedOutlineBrush?.Dispose();
        _cachedOutlineBrush = null;

        // 9. 清理调试渲染器与表情管理器
        _debugRenderer?.Dispose();
        _debugRenderer = null;
        _emojiManager?.Dispose();

        // 10. 最后释放资源管理器（包含 DirectX 资源）
        _resourceManager?.Dispose();

        Logger.Log("DanmakuDirectXRender is disposed");
    }

    private void RenderDebugInfo(int totalCount)
    {
        if (CanvasWidth == 0 || _resourceManager is null)
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

    /// <summary>
    /// 获取当前所有层的活跃弹幕总数.
    /// </summary>
    private int GetActiveDanmakuCount()
    {
        if (_renderLayerList is null)
        {
            return 0;
        }

        int count = 0;
        foreach (RenderLayer layer in _renderLayerList)
        {
            count += layer.RenderList.Count;
        }

        return count;
    }

    #region 独立自主渲染循环实现

    /// <summary>
    /// 渲染帧的主方法（在专用高优先级渲染线程循环调用）.
    /// </summary>
    private void RenderFrame()
    {
        if (_isPaused || _isStopped)
        {
            return;
        }

        try
        {
            lock (_resourceManager.RenderLock)
            {
                if (
                    _isPaused
                    || _isStopped
                    || _resourceManager.D2DContext is null
                    || _resourceManager.SwapChain is null
                )
                {
                    return;
                }

                int activeCount = GetActiveDanmakuCount();
                if (activeCount == 0)
                {
                    if (_hasClearedScreen)
                    {
                        // 屏幕已清空，且当前无弹幕：重置上一帧时间戳，进入节能等待
                        _lastFrameTime = _frameStopwatch.Elapsed;
                        _smoothedElapsedMs = 0f;
                        _gameLoopThread?.WaitForWork(40);
                        return;
                    }
                }

                TimeSpan currentTime = _frameStopwatch.Elapsed;
                TimeSpan rawElapsedTime = currentTime - _lastFrameTime;
                _lastFrameTime = currentTime;

                float rawMs = (float)rawElapsedTime.TotalMilliseconds;
                if (rawMs > 100.0f || rawMs <= 0.0f || _smoothedElapsedMs <= 0.0f)
                {
                    rawMs = 16.666f;
                    _smoothedElapsedMs = 16.666f;
                }
                else
                {
                    // 🚀 核心优化：时间步长低通滤波平滑 (EMA Filter)
                    // 消除由于 Windows 线程调度微抖动产生的帧时间波动，保证每一帧弹幕移动距离完全均匀恒定
                    _smoothedElapsedMs = (_smoothedElapsedMs * 0.85f) + (rawMs * 0.15f);
                }

                TimeSpan smoothedElapsedTime = TimeSpan.FromMilliseconds(_smoothedElapsedMs);
                _totalTime += smoothedElapsedTime;

                // 更新性能统计
                UpdatePerformanceStats(rawElapsedTime);

                if (activeCount > 0)
                {
                    _hasClearedScreen = false;
                    // 更新弹幕位置（使用平滑后的时间，确保亚像素运动绝对均匀）
                    UpdateDanmaku(smoothedElapsedTime, _totalTime);
                }
                else
                {
                    // 标记当前帧为清屏帧
                    _hasClearedScreen = true;
                }

                // 渲染弹幕
                RenderDanmaku();

                // 🚀 硬件垂直同步呈现：Present(1, PresentFlags.None)
                // 1. 由显卡/DWM 硬件垂直同步驱动，精准匹配显示器物理刷新率（60Hz/120Hz/144Hz/240Hz）
                // 2. 在后台独立高优先级线程阻塞等待硬件 V-Blank，完全避开 WinUI 3 UI 消息循环抖动
                Result? presentResult = _resourceManager.SwapChain?.Present(1, PresentFlags.None);
                if (presentResult is
                    {
                        Failure: true
                    })
                {
                    Logger.Log($"Present failed: {presentResult.Value}");
                }

                // 防止窗口遮挡或极少情况下 Present 立即返回导致紧密忙等
                if (rawMs < 1.0f)
                {
                    Thread.Sleep(1);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Render error: {ex.Message}");
        }
    }

    #endregion
}
