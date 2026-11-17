using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.Components;
using WinUI3Package;
using WinUIKernel.Share;

namespace PhoenixReadCopilot.Controls.Favorite;

/// <summary>
/// 视频收藏主体.
/// </summary>
public sealed partial class VideoFavoriteBody : VideoFavoriteControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoFavoriteBody"/> class.
    /// </summary>
    public VideoFavoriteBody()
    {
        InitializeComponent();
    }

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
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                VideoScrollView.ExtentHeight
                    - VideoScrollView.ViewportHeight
                    - VideoScrollView.VerticalOffset
                <= 240
            )
            {
                _ = ViewModel.LoadItemsAsync();
            }
        });
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
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (VideoScrollView.ScrollableHeight <= 240 && ViewModel is not null)
            {
                _ = ViewModel.LoadItemsAsync();
            }
        });
    }

    private void VideoRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, GridViewRevealFocusPanel);

            CompositionFactory.DisableStandardReposition(element);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void VideoRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            FavoriteVideoItemTemplate.ClearImage(element);
        }
    }
}
