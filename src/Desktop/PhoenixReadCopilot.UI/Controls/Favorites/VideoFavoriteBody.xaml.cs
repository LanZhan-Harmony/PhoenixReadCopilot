using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.UI.ViewModels.Components;
using WinUIKernel.Share;

namespace PhoenixReadCopilot.UI.Controls.Favorites;

/// <summary>
/// 视频收藏主体.
/// </summary>
public sealed partial class VideoFavoriteBody : VideoFavoriteControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoFavoriteBody"/> class.
    /// </summary>
    public VideoFavoriteBody() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        VideoScrollView.ViewChanged += OnViewChanged;
        VideoScrollView.SizeChanged += OnScrollViewSizeChanged;
        if (ViewModel is null)
        {
            return;
        }

        CheckVideoCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ViewModel?.ListUpdated -= OnVideoListUpdatedAsync;

        VideoRepeater.ItemsSource = null;
        VideoScrollView.ViewChanged -= OnViewChanged;
        VideoScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        VideoFavoriteSectionDetailViewModel? oldValue,
        VideoFavoriteSectionDetailViewModel? newValue
    )
    {
        oldValue?.ListUpdated -= OnVideoListUpdatedAsync;

        if (newValue is null)
        {
            return;
        }

        newValue.ListUpdated += OnVideoListUpdatedAsync;
    }

    private async void OnVideoListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckVideoCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        WinUIKernelShareExtensions.IsCardAnimationEnabled = !args.IsIntermediate;
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (
                    VideoScrollView.ExtentHeight
                        - VideoScrollView.ViewportHeight
                        - VideoScrollView.VerticalOffset
                    <= 240
                )
                {
                    ViewModel.LoadItemsCommand.Execute(default);
                }
            }
        );
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100 && ViewModel is not null)
        {
            CheckVideoCount();
        }
    }

    private void CheckVideoCount()
    {
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (VideoScrollView.ScrollableHeight <= 240 && ViewModel is not null)
                {
                    ViewModel.LoadItemsCommand.Execute(default);
                }
            }
        );
    }
}
