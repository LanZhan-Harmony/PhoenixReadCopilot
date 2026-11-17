using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.History;

/// <summary>
/// 历史记录视频搜索部分.
/// </summary>
public sealed partial class HistoryVideoSearchSection : HistoryPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryVideoSearchSection"/> class.
    /// </summary>
    public HistoryVideoSearchSection()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ViewModel.SearchUpdated += OnSearchUpdatedAsync;
        VideoScrollView.ViewChanged += OnViewChanged;
        VideoScrollView.SizeChanged += OnScrollViewSizeChanged;

        CheckVideoCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        VideoRepeater.ItemsSource = null;
        ViewModel.SearchUpdated -= OnSearchUpdatedAsync;
        VideoScrollView.ViewChanged -= OnViewChanged;
        VideoScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private async void OnSearchUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckVideoCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                VideoScrollView.ExtentHeight
                    - VideoScrollView.ViewportHeight
                    - VideoScrollView.VerticalOffset
                <= 240
            )
            {
                _ = ViewModel.LoadMoreSearchResultAsync();
            }
        });
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100)
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
                _ = ViewModel.LoadMoreSearchResultAsync();
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
            HistoryVideoItemTemplate.ClearImage(element);
        }
    }
}
