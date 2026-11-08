using Danmaku.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Danmaku.Core;

/// <summary>
/// 弹幕渲染器 - 统一的公开 API，整合渲染引擎和调度器.
/// </summary>
public sealed class DanmakuRenderer : IDisposable
{
    public const uint DefaultDanmakuLayerCount = DanmakuDefaultLayerDef.DefaultLayerCount;

    private readonly DanmakuDirectXRender _render;
    private readonly DanmakuScheduler _scheduler;
    private readonly Grid _rootGrid;
    private readonly SwapChainPanel _swapChainPanel;
    private bool _isDisposed;

    /// <summary>
    /// 调试模式 - 显示性能信息.
    /// </summary>
    public bool DebugMode
    {
        get => _render.DebugMode;
        set => _render.DebugMode = value;
    }

    /// <summary>
    /// 创建弹幕渲染器（必须在 UI 线程调用）.
    /// </summary>
    /// <param name="rootGrid">根容器.</param>
    /// <param name="logger">日志记录器（可选）.</param>
    /// <param name="initialConfig">初始配置（可选）.</param>
    public DanmakuRenderer(
        Grid rootGrid,
        ILogger<DanmakuRenderer> logger = default,
        DanmakuConfiguration initialConfig = null
    )
    {
        _rootGrid = rootGrid ?? throw new ArgumentNullException(nameof(rootGrid));

        // 移除容器内已有的旧 SwapChainPanel，杜绝堆积废弃面板
        for (int i = _rootGrid.Children.Count - 1; i >= 0; i--)
        {
            if (_rootGrid.Children[i] is SwapChainPanel)
            {
                _rootGrid.Children.RemoveAt(i);
            }
        }

        if (_rootGrid.XamlRoot is null)
        {
            throw new InvalidOperationException(
                "rootGrid.XamlRoot is null - make sure the Grid is attached to the visual tree"
            );
        }

        _swapChainPanel = new SwapChainPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _rootGrid.Children.Add(_swapChainPanel);

        Logger.SetLogger(logger);

        // 创建渲染引擎
        _render = new DanmakuDirectXRender(_swapChainPanel, _rootGrid, initialConfig);

        // 创建调度器
        _scheduler = new DanmakuScheduler();

        // 连接调度器事件
        _scheduler.DanmakuRendered += OnDanmakuRendered;
        _scheduler.PauseRequested += OnPauseRequested;
        _scheduler.ResumeRequested += OnResumeRequested;
        _scheduler.StopRequested += OnStopRequested;
    }

    #region 配置管理

    /// <summary>
    /// 更新弹幕配置.
    /// </summary>
    /// <param name="config">新的配置（只有非空值会被应用）.</param>
    public void UpdateConfiguration(DanmakuConfiguration config)
    {
        _render?.UpdateConfiguration(config);
    }

    #endregion

    #region 播放控制

    /// <summary>
    /// 设置弹幕类型的启用状态.
    /// </summary>
    public void SetDanmakuEnabledType(
        bool? isRollingEnabled,
        bool? isTopEnabled,
        bool? isBottomEnabled
    )
    {
        _scheduler?.SetDanmakuEnabledType(isRollingEnabled, isTopEnabled, isBottomEnabled);
    }

    /// <summary>
    /// 更新当前播放时间.
    /// </summary>
    public void UpdateTime(uint currentMs)
    {
        _scheduler?.UpdateTime(currentMs);
    }

    /// <summary>
    /// 暂停弹幕.
    /// </summary>
    public void Pause()
    {
        _scheduler?.Pause();
    }

    /// <summary>
    /// 恢复弹幕.
    /// </summary>
    public void Resume()
    {
        _scheduler?.Resume();
    }

    /// <summary>
    /// 停止弹幕.
    /// </summary>
    public void Stop()
    {
        _scheduler?.Stop();
    }

    /// <summary>
    /// 重新开始（从0开始）.
    /// </summary>
    public void Restart()
    {
        _scheduler?.Restart();
    }

    /// <summary>
    /// 跳转到指定时间.
    /// </summary>
    public void Seek(uint targetMs)
    {
        _scheduler?.Seek(targetMs);
    }

    #endregion

    #region 图层管理

    /// <summary>
    /// 设置图层的渲染状态.
    /// </summary>
    public void SetLayerRenderState(uint layerId, bool render)
    {
        _render?.SetLayerRenderState(layerId, render);
    }

    #endregion

    #region 弹幕管理

    /// <summary>
    /// 清空弹幕列表与屏幕上正在飞行的弹幕节点.
    /// </summary>
    public void Clear()
    {
        _scheduler?.Clear();
        _render?.Clear();
    }

    /// <summary>
    /// 添加实时弹幕.
    /// </summary>
    /// <param name="item">弹幕项.</param>
    /// <param name="insertToList">是否插入到弹幕列表.</param>
    /// <param name="layerId">图层 ID（可选，默认使用弹幕模式对应的图层）.</param>
    public void AddRealtimeDanmaku(DanmakuItem item, bool insertToList, uint? layerId = null)
    {
        if (_render is null || _scheduler is null)
        {
            return;
        }

        _scheduler.AddRealtimeDanmaku(item, insertToList);
    }

    /// <summary>
    /// 获取弹幕列表.
    /// </summary>
    public List<DanmakuItem> GetDanmakuList()
    {
        return _scheduler?.GetDanmakuList() ?? new List<DanmakuItem>();
    }

    /// <summary>
    /// 设置弹幕列表.
    /// </summary>
    public void SetDanmakuList(List<DanmakuItem> danmakuList)
    {
        _scheduler?.SetDanmakuList([.. danmakuList.OrderBy(item => item.StartMs)]);
    }

    /// <summary>
    /// 添加弹幕列表（去重）.
    /// </summary>
    public void AddDanmakuList(List<DanmakuItem> danmakuList)
    {
        _scheduler?.AddDanmakuList(danmakuList);
    }

    #endregion

    #region 生命周期

    /// <summary>
    /// 释放资源.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // 关键修复：按正确顺序释放，避免竞态条件
        // 1. 先停止调度器（停止产生新弹幕）
        _scheduler?.Dispose();

        // 2. 再释放渲染器（停止渲染循环，释放 DirectX 资源）
        _render?.Dispose();

        // 3. 从根容器中移除已释放的 SwapChainPanel，避免残存死画板
        if (_swapChainPanel != null && _rootGrid != null)
        {
            _rootGrid.Children.Remove(_swapChainPanel);
        }

        // 4. 解除事件订阅，防止内存泄漏
        if (_scheduler != null)
        {
            _scheduler.DanmakuRendered -= OnDanmakuRendered;
            _scheduler.PauseRequested -= OnPauseRequested;
            _scheduler.ResumeRequested -= OnResumeRequested;
            _scheduler.StopRequested -= OnStopRequested;
        }
    }

    #endregion

    #region 事件处理

    private void OnDanmakuRendered(uint layerId, DanmakuItem item)
    {
        _render?.RenderDanmakuItem(layerId, item);
    }

    private void OnPauseRequested()
    {
        _render?.Pause();
    }

    private void OnResumeRequested()
    {
        _render?.Start();
    }

    private void OnStopRequested()
    {
        _render?.Stop();
    }

    #endregion
}
