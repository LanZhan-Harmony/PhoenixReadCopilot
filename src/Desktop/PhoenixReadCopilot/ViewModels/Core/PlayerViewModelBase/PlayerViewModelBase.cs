using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Player;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.Media;
using Windows.Storage.Streams;
using WinUIEx;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 播放器视图模型基类.
/// </summary>
public abstract partial class PlayerViewModelBase : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerViewModelBase"/> class.
    /// </summary>
    protected PlayerViewModelBase(
        ISettingsService settingsService,
        AppViewModel appViewModel,
        ILogger<PlayerViewModelBase> logger,
        DispatcherQueue dispatcherQueue
    )
    {
        _settings = settingsService.Settings;
        _appViewModel = appViewModel;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
    }

    /// <summary>
    /// 初始化系统播放控制器.
    /// </summary>
    public void InitializeSmtc(string cover, string title)
    {
        if (_smtc is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(cover))
        {
            Cover = new Uri(cover);
        }

        SystemMediaTransportControlsDisplayUpdater updater = _smtc.DisplayUpdater;
        updater.ClearAll();
        updater.Type = MediaPlaybackType.Video;
        if (!string.IsNullOrEmpty(cover))
        {
            updater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(cover));
        }

        updater.VideoProperties.Title = title;
        updater.Update();
    }

    /// <summary>
    /// 设置播放数据.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    public virtual async Task SetPlayDataAsync(
        string? videoUrl,
        string? audioUrl,
        bool isAutoPlay,
        int position = 0,
        string? contentType = null,
        string? extraOptions = null
    )
    {
        _isClosed = false;
        if (_smtc is null)
        {
            nint currentWindow = _appViewModel.ActivatedWindow.GetWindowHandle();
            _smtc = SystemMediaTransportControlsInterop.GetForWindow(currentWindow);
            _smtc.IsEnabled = true;
            _smtc.IsPlayEnabled = true;
            _smtc.IsPauseEnabled = true;
            _smtc.ButtonPressed += OnSystemControlsButtonPressedAsync;

            // 独立窗口播放时默认全窗口播放.
            PlayerDisplayMode defaultDisplay = _settings.DefaultPlayerDisplayMode;
            if (!IsFullScreen && defaultDisplay == PlayerDisplayMode.FullScreen)
            {
                ToggleFullScreenCommand.Execute(null);
            }
            else if (!IsCompactOverlay && defaultDisplay == PlayerDisplayMode.CompactOverlay)
            {
                ToggleCompactOverlayCommand.Execute(null);
            }
            else if (!IsFullWindow && defaultDisplay == PlayerDisplayMode.FullWindow)
            {
                ToggleFullWindowCommand.Execute(null);
            }
        }

        if (IsMediaLoaded())
        {
            await PauseAsync();
        }

        _videoUrl = videoUrl;
        _audioUrl = audioUrl;
        _autoPlay = isAutoPlay;
        Position = position;
        _contentType = contentType;
        _extraOptions = extraOptions;

        CancelNotification();

        bool isSpeedShare = _settings.IsPlayerSpeedShare;
        double localSpeed = _settings.PlayerSpeed;
        Volume = _settings.PlayerVolume;
        Speed = isSpeedShare ? localSpeed : 1.0;
        bool isSpeedEnhancement = _settings.IsPlayerSpeedEnhancement;
        MaxSpeed = isSpeedEnhancement ? 6.0 : 3.0;
        SpeedStep = isSpeedEnhancement ? 0.1 : 0.25;

        if (_isInitialized && !_isClosed)
        {
            BeforeLoadPlayData();
            await TryLoadPlayDataAsync();
        }
    }

    /// <summary>
    /// 注入进度改变时的回调.
    /// </summary>
    public void SetProgressAction(Action<int, int> action) => _progressAction = action;

    /// <summary>
    /// 注入窗口状态改变时的回调.
    /// </summary>
    public void SetWindowStateChangeAction(Action action) => _windowStateChangeAction = action;

    /// <summary>
    /// 注入状态改变时的回调.
    /// </summary>
    public void SetStateAction(Action<PlayerState> action) => _stateAction = action;

    /// <summary>
    /// 注入速度改变时的回调.
    /// </summary>
    public void SetSpeedAction(Action<double> action) => _speedAction = action;

    /// <summary>
    /// 注入播放结束时的回调.
    /// </summary>
    public void SetEndAction(Action action) => _endAction = action;

    /// <summary>
    /// 注入重新加载时的回调.
    /// </summary>
    public void SetReloadAction(Action action) => _reloadAction = action;

    /// <summary>
    /// 注入检查输入框是否聚焦的回调.
    /// </summary>
    public void SetIsTextBoxFocusedFunc(Func<bool> func) => _isTextBoxFocusedFunc = func;

    /// <summary>
    /// 关闭播放器.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    public Task CloseAsync()
    {
        _isClosed = true;
        AttachedWindow = null;
        if (_smtc is not null)
        {
            _smtc.DisplayUpdater.ClearAll();
            _smtc.IsEnabled = false;
            _smtc.ButtonPressed -= OnSystemControlsButtonPressedAsync;
            _smtc = null;
        }

        ReleaseDisplay();
        return OnCloseAsync();
    }

    /// <summary>
    /// 显示通知.
    /// </summary>
    public void ShowNotification(PlayerNotification notification)
    {
        PlayerNotificationItemViewModel item = new(notification);
        RequestShowNotification?.Invoke(this, item);
    }

    /// <summary>
    /// 取消通知.
    /// </summary>
    public void CancelNotification() => RequestCancelNotification?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// 检查底部进度条是否可见.
    /// </summary>
    /// <param name="shouldShow">是否需要显示.</param>
    public void CheckBottomProgressVisibility(bool shouldShow)
    {
        bool isEnabled = _settings.IsBottomProgressVisible;
        IsBottomProgressVisible = isEnabled && shouldShow;
    }

    /// <summary>
    /// 在加载播放数据之前.
    /// </summary>
    protected virtual void BeforeLoadPlayData() { }

    /// <summary>
    /// 准备关闭.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    protected virtual Task OnCloseAsync() => Task.CompletedTask;

    /// <summary>
    /// 尝试加载播放数据.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    protected abstract Task OnLoadPlayDataAsync();

    partial void OnIsFullScreenChanged(bool value)
    {
        if (value && IsCompactOverlay)
        {
            IsCompactOverlay = false;
        }
        else if (value && IsFullWindow)
        {
            IsFullWindow = false;
        }
    }

    partial void OnIsCompactOverlayChanged(bool value)
    {
        if (value && IsFullScreen)
        {
            IsFullScreen = false;
        }
        else if (value && IsFullWindow)
        {
            IsFullWindow = false;
        }
    }

    partial void OnIsFullWindowChanged(bool value)
    {
        if (value && IsFullScreen)
        {
            IsFullScreen = false;
        }
        else if (value && IsCompactOverlay)
        {
            IsCompactOverlay = false;
        }
    }

    /// <summary>
    /// 释放当前播放的媒体源（用于切集前彻底停止旧视频，避免网络请求期间继续播放）.
    /// </summary>
    public virtual void ReleaseCurrentSource()
    {
    }
}
