using System.Diagnostics;
using System.Numerics;
using Danmaku.Core.Models;
using Microsoft.UI.Xaml.Controls;
using Vortice.Direct2D1;

namespace Danmaku.Core;

/// <summary>
/// DanmakuDirectXRender 的字段定义部分.
/// 包含常量、私有字段和静态字段.
/// </summary>
internal partial class DanmakuDirectXRender
{
    #region 常量定义

    /// <summary>
    /// 底部和顶部弹幕默认持续时间（毫秒）.
    /// </summary>
    private const float DefaultBottomAndTopDurationMs = 3000f;

    #endregion

    #region UI 和资源管理

    /// <summary>
    /// SwapChainPanel 控件引用.
    /// </summary>
    private SwapChainPanel _swapChainPanel;

    /// <summary>
    /// DirectX 资源管理器，负责管理所有 DirectX 资源.
    /// </summary>
    private readonly DirectXResourceManager _resourceManager;

    /// <summary>
    /// 调试信息渲染器.
    /// </summary>
    private DebugRenderer _debugRenderer;

    #endregion

    #region 渲染层和状态管理

    /// <summary>
    /// 渲染层列表，每层独立管理弹幕.
    /// </summary>
    private readonly RenderLayer[] _renderLayerList;

    /// <summary>
    /// 标记渲染是否已停止.
    /// </summary>
    private volatile bool _isStopped;

    /// <summary>
    /// 标记渲染是否已暂停.
    /// </summary>
    private volatile bool _isPaused;

    /// <summary>
    /// 重入保护标志：防止在处理一帧时再次进入渲染.
    /// 关键修复：避免 OnRendering 与 UI 事件（Loading/Measure）冲突导致闪退.
    /// </summary>
    private volatile bool _isRendering;

    /// <summary>
    /// DPI 值（每英寸点数）.
    /// </summary>
    private float _dpi;

    /// <summary>
    /// 缩放比例（用于高 DPI 显示）.
    /// </summary>
    private float _scale = 1.0f;

    #endregion

    #region 弹幕配置参数

    /// <summary>
    /// 弹幕配置状态，统一管理所有配置参数.
    /// </summary>
    private readonly DanmakuConfigurationState _configState;

    /// <summary>
    /// 最大弹幕数量限制.
    /// </summary>
    private readonly int _maxDanmakuSize = 16384;

    #endregion

    #region 性能优化选项

    /// <summary>
    /// 是否启用纹理缓存（用于调试和性能对比）.
    /// </summary>
    private readonly bool _enableTextureCache = true;

    #endregion

    #region 异步任务和状态管理

    /// <summary>
    /// 游戏循环线程，用于管理渲染循环.
    /// </summary>
    private GameLoopThread? _gameLoopThread;

    #endregion

    #region 时间和性能统计

    /// <summary>
    /// 帧计时秒表.
    /// </summary>
    private readonly Stopwatch _frameStopwatch = new();

    /// <summary>
    /// 累计运行时间.
    /// </summary>
    private TimeSpan _totalTime = TimeSpan.Zero;

    /// <summary>
    /// 上一帧的时间戳.
    /// </summary>
    private TimeSpan _lastFrameTime = TimeSpan.Zero;

    /// <summary>
    /// 当前帧率（使用指数移动平均平滑处理）.
    /// </summary>
    private double _currentFrameRate = 60.0;

    #endregion

    #region DirectX 资源缓存

    /// <summary>
    /// 缓存的描边画刷.
    /// </summary>
    private ID2D1SolidColorBrush _cachedOutlineBrush;

    #endregion

    #region 优化：预分配和可重用对象

    /// <summary>
    /// 可重用的 Vector2 实例（减少 GC 分配）.
    /// </summary>
    private Vector2 _reusableVector2;

    /// <summary>
    /// 可重用的 Matrix3x2 实例（减少 GC 分配）.
    /// </summary>
    private Matrix3x2 _reusableMatrix;

    /// <summary>
    /// 预计算的 8 方向描边偏移量（上下左右 + 四个对角线）.
    /// 确保笔画转折点也有描边覆盖，避免马赛克感.
    /// </summary>
    private static readonly Vector2[] _outlineOffsets = new Vector2[8];

    /// <summary>
    /// 透明背景色（用于清屏）.
    /// </summary>
    private static readonly Vortice.Mathematics.Color4 _clearColor = new(0, 0, 0, 0);

    /// <summary>
    /// 🚀 GC 优化：预分配快照数组，避免每帧 ToArray() 分配.
    /// 每层一个数组，容量动态调整.
    /// </summary>
    private readonly DanmakuDirectXRenderItem[][] _renderSnapshots;

    #endregion

    #region 静态构造函数

    /// <summary>
    /// 静态构造函数，初始化静态字段.
    /// </summary>
    static DanmakuDirectXRender()
    {
        // 预计算 8 方向描边偏移量（确保转折点无缝隙）
        _outlineOffsets[0] = new Vector2(-1, 0); // 左
        _outlineOffsets[1] = new Vector2(1, 0); // 右
        _outlineOffsets[2] = new Vector2(0, -1); // 上
        _outlineOffsets[3] = new Vector2(0, 1); // 下
        _outlineOffsets[4] = new Vector2(-1, -1); // 左上
        _outlineOffsets[5] = new Vector2(1, -1); // 右上
        _outlineOffsets[6] = new Vector2(-1, 1); // 左下
        _outlineOffsets[7] = new Vector2(1, 1); // 右下
    }

    #endregion

    #region 公共属性

    /// <summary>
    /// 画布宽度（物理像素）.
    /// </summary>
    public float CanvasWidth { get; private set; }

    /// <summary>
    /// 画布高度（物理像素）.
    /// </summary>
    public float CanvasHeight { get; private set; }

    /// <summary>
    /// 是否启用调试模式.
    /// </summary>
    public bool DebugMode { get; set; }

    #endregion
}
