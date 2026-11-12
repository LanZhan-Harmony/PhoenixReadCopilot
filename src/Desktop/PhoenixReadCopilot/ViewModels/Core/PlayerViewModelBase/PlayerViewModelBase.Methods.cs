using Windows.System.Display;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 播放器视图模型基类.
/// </summary>
public abstract partial class PlayerViewModelBase
{
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
