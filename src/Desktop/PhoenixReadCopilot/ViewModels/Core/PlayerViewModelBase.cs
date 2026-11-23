using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Controls.Components;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Player;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Display;
using WinUIEx;
using ZLogger;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 播放器视图模型基类.
/// </summary>
internal abstract partial class PlayerViewModelBase : ViewModelBase
{
    protected readonly ILogger<PlayerViewModelBase> _logger;
    protected readonly DispatcherQueue _dispatcherQueue;
    protected readonly AppViewModel _appViewModel;

    protected readonly AppSettings _settings;

    protected string? _videoUrl;
    protected string? _audioUrl;
    protected bool _autoPlay;
    protected string? _contentType;

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
                ToggleFullScreen();
            }
            else if (!IsCompactOverlay && defaultDisplay == PlayerDisplayMode.CompactOverlay)
            {
                ToggleCompactOverlay();
            }
            else if (!IsFullWindow && defaultDisplay == PlayerDisplayMode.FullWindow)
            {
                ToggleFullWindow();
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
    public virtual void ReleaseCurrentSource() { }

    /// <summary>
    /// 尝试切换播放/暂停. 如果此时输入框获得焦点，则不执行.
    /// </summary>
    /// <returns>结果.</returns>
    public bool TryTogglePlayPause()
    {
        if (_isTextBoxFocusedFunc?.Invoke() ?? false)
        {
            return false;
        }

        TogglePlayPause();
        IsPaused = !IsPaused;
        return true;
    }

    /// <summary>
    /// 媒体是否就绪.
    /// </summary>
    /// <returns>结果.</returns>
    protected abstract bool IsMediaLoaded();

    /// <summary>
    /// 切换播放/暂停.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    protected abstract Task OnTogglePlayPauseAsync();

    /// <summary>
    /// 强制播放.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    protected abstract Task ForcePlayAsync();

    /// <summary>
    /// 跳转到指定位置.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    protected abstract Task OnSeekAsync(TimeSpan position);

    /// <summary>
    /// 执行暂停操作.
    /// </summary>
    protected virtual Task OnPauseAsync() => Task.CompletedTask;

    /// <summary>
    /// 设置音量.
    /// </summary>
    protected abstract void OnSetVolume(int value);

    /// <summary>
    /// 设置播放速度.
    /// </summary>
    protected abstract void OnSetSpeed(double speed);

    /// <summary>
    /// 尝试截图.
    /// </summary>
    /// <param name="path">指定保存的文件路径.</param>
    /// <returns><see cref="Task"/>.</returns>
    protected abstract Task OnTakeScreenshotAsync(string path);

    public async Task RestartAsync()
    {
        await SeekAsync(0);
        await ForcePlayAsync();
    }

    /// <summary>
    /// 重新播放 (同步触发).
    /// </summary>
    public void Restart() => _ = RestartAsync();

    /// <summary>
    /// 暂停播放.
    /// </summary>
    public async Task PauseAsync()
    {
        if (IsMediaLoaded())
        {
            await OnPauseAsync();
        }
    }

    [RelayCommand]
    public async Task TogglePlayPauseAsync()
    {
        if (IsPaused && !IsMediaLoaded() && Math.Abs(Position - Duration) <= 1)
        {
            await SetPlayDataAsync(_videoUrl, _audioUrl, true, 0, _contentType, _extraOptions);
            return;
        }

        await OnTogglePlayPauseAsync();
    }

    /// <summary>
    /// 切换播放/暂停 (同步触发).
    /// </summary>
    public void TogglePlayPause() => _ = TogglePlayPauseAsync();

    public async Task SeekAsync(double value)
    {
        if (Math.Abs(value - Position) < 3 || IsPlayerDataLoading || value > Duration)
        {
            return;
        }

        // 如果媒体已经播放结束，尝试重新加载媒体.
        if (!IsMediaLoaded())
        {
            await SetPlayDataAsync(
                _videoUrl,
                _audioUrl,
                _autoPlay,
                Convert.ToInt32(value),
                _contentType,
                _extraOptions
            );
            return;
        }

        try
        {
            await OnSeekAsync(TimeSpan.FromSeconds(value));
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"尝试跳转视频时失败.");
        }
    }

    /// <summary>
    /// 跳转播放进度 (同步触发).
    /// </summary>
    public void Seek(double value) => _ = SeekAsync(value);

    public void SetVolume(int value)
    {
        if (value < 0)
        {
            value = 0;
        }
        else if (value > 100)
        {
            value = 100;
        }

        OnSetVolume(value);
        if (Volume != value)
        {
            Volume = value;
            _settings.PlayerVolume = value;
        }
    }

    public void SetSpeed(double speed)
    {
        if (speed > MaxSpeed)
        {
            return;
        }

        OnSetSpeed(speed);
        if (Speed != speed)
        {
            Speed = speed;
            bool isShareSpeed = _settings.IsPlayerSpeedShare;
            if (isShareSpeed)
            {
                _settings.PlayerSpeed = speed;
            }
        }
    }

    [RelayCommand]
    public void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
        if (IsFullScreen)
        {
            _appViewModel.MakeCurrentWindowEnterFullScreen();
        }
        else
        {
            _appViewModel.MakeCurrentWindowExitFullScreen();
        }

        _windowStateChangeAction?.Invoke();
    }

    [RelayCommand]
    public void ToggleFullWindow()
    {
        IsFullWindow = !IsFullWindow;
        if (IsFullWindow)
        {
            _appViewModel.MakeCurrentWindowEnterOverlap();
        }
        else
        {
            _appViewModel.MakeCurrentWindowExitOverlap();
        }

        _windowStateChangeAction?.Invoke();
    }

    [RelayCommand]
    public void ToggleCompactOverlay()
    {
        IsCompactOverlay = !IsCompactOverlay;
        if (IsCompactOverlay)
        {
            _appViewModel.MakeCurrentWindowEnterCompactOverlay();
        }
        else
        {
            _appViewModel.MakeCurrentWindowExitCompactOverlay();
        }

        _windowStateChangeAction?.Invoke();
    }

    [RelayCommand]
    public void BackToDefaultMode()
    {
        if (IsFullScreen)
        {
            ToggleFullScreen();
        }
        else if (IsFullWindow)
        {
            ToggleFullWindow();
        }
        else if (IsCompactOverlay)
        {
            ToggleCompactOverlay();
        }
    }

    [RelayCommand]
    public void ForwardSkip()
    {
        double seconds = _settings.SingleFastForwardAndRewindSpan;
        if (seconds <= 0 || !IsMediaLoaded())
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (Duration - Position < seconds)
            {
                Seek(Duration);
            }
            else
            {
                Seek(Position + seconds);
            }
        });
    }

    [RelayCommand]
    public void BackwardSkip()
    {
        double seconds = _settings.SingleFastForwardAndRewindSpan;
        if (seconds <= 0 || !IsMediaLoaded())
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (Position < seconds)
            {
                Seek(0);
            }
            else
            {
                Seek(Position - seconds);
            }
        });
    }

    [RelayCommand]
    private async Task TakeScreenshotAsync()
    {
        try
        {
            string fileName = DateTimeOffset.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";
            string path = Path.Combine(GetScreenshotFolderPath(), fileName);
            if (!Directory.Exists(GetScreenshotFolderPath()))
            {
                Directory.CreateDirectory(GetScreenshotFolderPath());
            }

            await OnTakeScreenshotAsync(path);
            if (File.Exists(path))
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                bool shouldCopyToClipboard = _settings.CopyAfterScreenshot;
                if (shouldCopyToClipboard)
                {
                    DataPackage dp = new();
                    dp.SetBitmap(
                        RandomAccessStreamReference.CreateFromFile(
                            await StorageFile.GetFileFromPathAsync(path)
                        )
                    );
                    Clipboard.SetContent(dp);
                    _appViewModel.ShowTip(Resources.Copied, InfoType.Success);
                }

                bool shouldOpenFile = _settings.OpenAfterScreenshot;
                if (shouldOpenFile)
                {
                    await Launcher.LaunchFileAsync(file).AsTask();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"尝试截图时失败.");
        }
    }

    [RelayCommand]
    private void Reload()
    {
        if (string.IsNullOrEmpty(_videoUrl) && string.IsNullOrEmpty(_audioUrl))
        {
            return;
        }

        _reloadAction?.Invoke();
    }

    [RelayCommand]
    private void PlayNext() => _playNextAction?.Invoke();

    [RelayCommand]
    private void PlayPrevious() => _playPreviousAction?.Invoke();

    /// <summary>
    /// 更新进度.
    /// </summary>
    protected virtual void UpdatePosition(int position, int? duration = null)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            Position = position;
            if (duration >= 0)
            {
                Duration = duration.Value;
            }

            _progressAction?.Invoke(Position, Duration);
        });
    }

    /// <summary>
    /// 更新状态.
    /// </summary>
    protected virtual void UpdateState(PlayerState state)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            if (_isClosed || _appViewModel.IsClosed)
            {
                await OnCloseAsync();
                return;
            }

            if (state == PlayerState.Playing)
            {
                IsPaused = false;
                IsBuffering = false;
                ActiveDisplay();
            }
            else if (state == PlayerState.Paused || state == PlayerState.None)
            {
                IsPaused = true;
                IsBuffering = false;
                ReleaseDisplay();
            }
            else
            {
                IsBuffering = state is PlayerState.Buffering;
                IsPaused = !IsBuffering;
            }

            _smtc?.PlaybackStatus =
                IsFailed ? MediaPlaybackStatus.Stopped
                : IsPaused ? MediaPlaybackStatus.Paused
                : MediaPlaybackStatus.Playing;

            IsFailed = state == PlayerState.Failed;
            _stateAction?.Invoke(state);
        });
    }

    /// <summary>
    /// 播放结束.
    /// </summary>
    protected virtual void ReachEnd() => _endAction?.Invoke();

    /// <summary>
    /// 初始化完成.
    /// </summary>
    protected void RaiseInitializedEvent() => Initialized?.Invoke(this, EventArgs.Empty);

    private void OnSystemControlsButtonPressedAsync(
        SystemMediaTransportControls sender,
        SystemMediaTransportControlsButtonPressedEventArgs args
    )
    {
        switch (args.Button)
        {
            case SystemMediaTransportControlsButton.Play:
            case SystemMediaTransportControlsButton.Pause:
                _dispatcherQueue.TryEnqueue(() =>
                {
                    TogglePlayPause();
                });
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 尝试加载播放数据.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    protected async Task TryLoadPlayDataAsync()
    {
        if ((string.IsNullOrEmpty(_videoUrl) && string.IsNullOrEmpty(_audioUrl)) || _isClosed)
        {
            return;
        }

        IsPlayerDataLoading = true;
        if (!_autoPlay)
        {
            IsPaused = true;
        }

        await OnLoadPlayDataAsync();

        PlayerDataLoaded?.Invoke(this, EventArgs.Empty);
        IsPlayerDataLoading = false;
    }

    private static string GetScreenshotFolderPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "PhoenixReadCopilot-Screenshots"
        );

    private void ActiveDisplay()
    {
        if (_displayRequest is not null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            _displayRequest = new DisplayRequest();
            _displayRequest.RequestActive();
        });
    }

    private void ReleaseDisplay()
    {
        if (_displayRequest is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            _displayRequest?.RequestRelease();
            _displayRequest = null;
        });
    }

    /// <summary>
    /// 设置播放下一集动作.
    /// </summary>
    public void SetPlayNextAction(Action action) => _playNextAction = action;

    /// <summary>
    /// 设置播放上一集动作.
    /// </summary>
    public void SetPlayPreviousAction(Action action) => _playPreviousAction = action;
}
