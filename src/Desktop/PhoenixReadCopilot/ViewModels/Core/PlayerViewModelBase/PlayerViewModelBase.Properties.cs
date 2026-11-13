using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.Media;
using Windows.System.Display;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 播放器视图模型基类.
/// </summary>
public abstract partial class PlayerViewModelBase
{
    protected readonly ILogger<PlayerViewModelBase> _logger;
    protected readonly DispatcherQueue _dispatcherQueue;
    protected readonly AppViewModel _appViewModel;

    protected readonly AppSettings _settings;

    protected string? _videoUrl;
    protected string? _audioUrl;
    protected bool _autoPlay;
    protected string? _contentType;
    protected string? _customReferer;

    /// <summary>
    /// 自定义 Referer.
    /// </summary>
    public string? CustomReferer { get; set; }

    /// <summary>
    /// CENC 解密密钥.
    /// </summary>
    public virtual string? DecryptionKey { get; set; }
    protected string? _extraOptions;
    protected Action<int, int> _progressAction;
    protected Action<PlayerState> _stateAction;
    protected Action<double> _speedAction;
    protected Action _endAction;
    protected Action _reloadAction;
    protected Action? _playNextAction;
    protected Action? _playPreviousAction;
    protected Action _windowStateChangeAction;
    protected DisplayRequest _displayRequest;
    protected Func<bool> _isTextBoxFocusedFunc;

    protected SystemMediaTransportControls? _smtc;

    protected bool _isInitialized;
    protected bool _isClosed;

    [ObservableProperty]
    public partial bool IsPlayerInitializing { get; set; }

    [ObservableProperty]
    public partial bool IsPlayerDataLoading { get; set; }

    [ObservableProperty]
    public partial bool IsPaused { get; set; } = true;

    [ObservableProperty]
    public partial bool IsFailed { get; set; }

    [ObservableProperty]
    public partial int Position { get; set; }

    [ObservableProperty]
    public partial int Duration { get; set; }

    [ObservableProperty]
    public partial int Volume { get; set; }

    [ObservableProperty]
    public partial double Speed { get; set; }

    [ObservableProperty]
    public partial double MaxSpeed { get; set; }

    [ObservableProperty]
    public partial double SpeedStep { get; set; }

    [ObservableProperty]
    public partial bool IsFullScreen { get; set; }

    [ObservableProperty]
    public partial bool IsCompactOverlay { get; set; }

    [ObservableProperty]
    public partial bool IsFullWindow { get; set; }

    [ObservableProperty]
    public partial bool IsBuffering { get; set; }

    [ObservableProperty]
    public partial bool IsBottomProgressVisible { get; set; }

    [ObservableProperty]
    public partial bool IsSeparatorWindowPlayer { get; set; }

    [ObservableProperty]
    public partial Uri? Cover { get; set; }

    [ObservableProperty]
    public partial bool IsDanmakuInputFocused { get; set; }

    /// <summary>
    /// 播放数据加载完成.
    /// </summary>
    public event EventHandler PlayerDataLoaded;

    /// <summary>
    /// 请求显示通知.
    /// </summary>
    public event EventHandler<PlayerNotificationItemViewModel> RequestShowNotification;

    /// <summary>
    /// 请求取消通知.
    /// </summary>
    public event EventHandler RequestCancelNotification;

    /// <summary>
    /// 初始化已完成.
    /// </summary>
    public event EventHandler Initialized;

    /// <summary>
    /// 视频标题.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 播放器关联的窗口.
    /// </summary>
    public Window AttachedWindow { get; set; }
}
