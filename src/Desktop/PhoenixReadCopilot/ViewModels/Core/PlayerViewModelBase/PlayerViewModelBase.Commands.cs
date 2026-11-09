using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Strings;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 播放器视图模型基类.
/// </summary>
public abstract partial class PlayerViewModelBase
{
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

        TogglePlayPauseCommand.Execute(null);
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

    [RelayCommand]
    private async Task RestartAsync()
    {
        await SeekAsync(0);
        await ForcePlayAsync();
    }

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
    private async Task TogglePlayPauseAsync()
    {
        if (IsPaused && !IsMediaLoaded() && Math.Abs(Position - Duration) <= 1)
        {
            await SetPlayDataAsync(_videoUrl, _audioUrl, true, 0, _contentType, _extraOptions);
            return;
        }

        await OnTogglePlayPauseAsync();
    }

    [RelayCommand]
    private async Task SeekAsync(double value)
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
            _logger.LogWarning(ex, "尝试跳转视频时失败.");
        }
    }

    [RelayCommand]
    private void SetVolume(int value)
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

    [RelayCommand]
    private void SetSpeed(double speed)
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
    private void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
        if (IsFullScreen)
        {
            _appViewModel.MakeCurrentWindowEnterFullScreenCommand.Execute(null);
        }
        else
        {
            _appViewModel.MakeCurrentWindowExitFullScreenCommand.Execute(null);
        }

        _windowStateChangeAction?.Invoke();
    }

    [RelayCommand]
    private void ToggleFullWindow()
    {
        IsFullWindow = !IsFullWindow;
        if (IsFullWindow)
        {
            _appViewModel.MakeCurrentWindowEnterOverlapCommand.Execute(null);
        }
        else
        {
            _appViewModel.MakeCurrentWindowExitOverlapCommand.Execute(null);
        }

        _windowStateChangeAction?.Invoke();
    }

    [RelayCommand]
    private void ToggleCompactOverlay()
    {
        IsCompactOverlay = !IsCompactOverlay;
        if (IsCompactOverlay)
        {
            _appViewModel.MakeCurrentWindowEnterCompactOverlayCommand.Execute(null);
        }
        else
        {
            _appViewModel.MakeCurrentWindowExitCompactOverlayCommand.Execute(null);
        }

        _windowStateChangeAction?.Invoke();
    }

    [RelayCommand]
    private void IncreaseSpeed()
    {
        double speed = Speed + 0.5;
        if (speed > MaxSpeed)
        {
            speed = MaxSpeed;
        }

        SetSpeed(speed);
    }

    [RelayCommand]
    private void DecreaseSpeed()
    {
        double speed = Speed - 0.5;
        if (speed < 0.5)
        {
            speed = 0.5;
        }

        SetSpeed(speed);
    }

    [RelayCommand]
    private void IncreaseVolume()
    {
        int volume = Volume + 5;
        if (volume > 100)
        {
            volume = 100;
        }

        SetVolume(volume);
    }

    [RelayCommand]
    private void DecreaseVolume()
    {
        int volume = Volume - 5;
        if (volume < 0)
        {
            volume = 0;
        }

        SetVolume(volume);
    }

    [RelayCommand]
    private void BackToDefaultMode()
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
    private void ForwardSkip()
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
                SeekCommand.Execute(Duration);
            }
            else
            {
                SeekCommand.Execute(Position + seconds);
            }
        });
    }

    [RelayCommand]
    private void BackwardSkip()
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
                SeekCommand.Execute(0);
            }
            else
            {
                SeekCommand.Execute(Position - seconds);
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
                    _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
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
            _logger.LogWarning(ex, "尝试截图时失败.");
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
}
