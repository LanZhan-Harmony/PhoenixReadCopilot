using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Models.Settings;
using Windows.UI;
using WinUIKernel.Share.Toolkits;
using DanmakuCoreItem = Danmaku.Core.Models.DanmakuItem;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 弹幕视图模型.
/// </summary>
public sealed partial class DanmakuViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly AppViewModel _appViewModel;
    private readonly ILogger<DanmakuViewModel> _logger;
    private readonly IFontToolkit _fontToolkit;
    private readonly AppSettings _settings;

    private string _seriesId = string.Empty;
    private string _videoId = string.Empty;
    private string _loadedVideoId = string.Empty;
    private int _position;
    private int _duration;
    private CancellationTokenSource? _loadCancellationTokenSource;

    /// <summary>
    /// 当前分集弹幕是否已成功加载.
    /// </summary>
    public bool HasLoaded => !string.IsNullOrEmpty(_loadedVideoId) && _loadedVideoId == _videoId;

    /// <summary>
    /// 当前已加载的弹幕列表缓存.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<DanmakuCoreItem> CurrentDanmakus { get; set; } = [];

    [ObservableProperty]
    public partial bool IsShowDanmaku { get; set; }

    [ObservableProperty]
    public partial bool CanShowDanmaku { get; set; }

    [ObservableProperty]
    public partial double DanmakuOpacity { get; set; }

    [ObservableProperty]
    public partial double DanmakuFontSize { get; set; }

    [ObservableProperty]
    public partial string DanmakuFontFamily { get; set; }

    [ObservableProperty]
    public partial double DanmakuArea { get; set; }

    [ObservableProperty]
    public partial double DanmakuSpeed { get; set; }

    [ObservableProperty]
    public partial double ExtraSpeed { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsDanmakuLimit { get; set; }

    [ObservableProperty]
    public partial bool IsDanmakuBold { get; set; }

    [ObservableProperty]
    public partial bool IsStandardSize { get; set; }

    [ObservableProperty]
    public partial DanmakuLocation Location { get; set; }

    [ObservableProperty]
    public partial Color Color { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial List<DanmakuLocation> Locations { get; set; }

    [ObservableProperty]
    public partial List<Color> Colors { get; set; }

    [ObservableProperty]
    public partial List<string> Fonts { get; set; }

    [ObservableProperty]
    public partial bool IsPaused { get; set; }

    /// <summary>
    /// 当弹幕列表添加时触发的事件.
    /// </summary>
    public event EventHandler<IReadOnlyList<DanmakuCoreItem>>? ListAdded;

    /// <summary>
    /// 速度加成改变时触发的事件.
    /// </summary>
    public event EventHandler? ExtraSpeedChanged;

    /// <summary>
    /// 当请求清除弹幕时触发的事件.
    /// </summary>
    public event EventHandler? RequestClearDanmaku;

    /// <summary>
    /// 当请求重绘弹幕时触发的事件.
    /// </summary>
    public event EventHandler? RequestRedrawDanmaku;

    /// <summary>
    /// 当进度更新时触发的事件.
    /// </summary>
    public event EventHandler<int>? ProgressChanged;

    /// <summary>
    /// 暂停弹幕.
    /// </summary>
    public event EventHandler? PauseDanmaku;

    /// <summary>
    /// 恢复弹幕.
    /// </summary>
    public event EventHandler? ResumeDanmaku;

    /// <summary>
    /// 请求添加单条弹幕.
    /// </summary>
    public event EventHandler<string>? RequestAddSingleDanmaku;

    /// <summary>
    /// 请求重置样式.
    /// </summary>
    public event EventHandler? RequestResetStyle;
}
