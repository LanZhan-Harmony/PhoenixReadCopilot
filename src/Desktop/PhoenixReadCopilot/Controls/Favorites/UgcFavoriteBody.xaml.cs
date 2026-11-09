using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;
using WinUIKernel.Share;

namespace PhoenixReadCopilot.Controls.Favorites;

/// <summary>
/// UGC收藏主体.
/// </summary>
public sealed partial class UgcFavoriteBody : UgcFavoriteControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UgcFavoriteBody"/> class.
    /// </summary>
    public UgcFavoriteBody() => InitializeComponent();

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
        UgcSeasonFavoriteSectionDetailViewModel? oldValue,
        UgcSeasonFavoriteSectionDetailViewModel? newValue
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
                    ViewModel.LoadItemsCommand.Execute(null);
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
                    ViewModel.LoadItemsCommand.Execute(null);
                }
            }
        );
    }
}
