using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers.Animations;
using PhoenixReadCopilot.ViewModels.View;
using WinUI3Package;

namespace PhoenixReadCopilot.Controls.User;

/// <summary>
/// 用户空间主体内容展示控件.
/// </summary>
public sealed partial class UserMainBody : UserControlBase
{
    public UserMainBody()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        VideoScrollView.ViewChanged += OnScrollViewerViewChanged;
        ReviewScrollView.ViewChanged += OnScrollViewerViewChanged;
        CircleScrollView.ViewChanged += OnScrollViewerViewChanged;
        RelationScrollView.ViewChanged += OnScrollViewerViewChanged;

        ViewModel?.DataUpdated += OnDataUpdated;
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        VideoScrollView.ViewChanged -= OnScrollViewerViewChanged;
        ReviewScrollView.ViewChanged -= OnScrollViewerViewChanged;
        CircleScrollView.ViewChanged -= OnScrollViewerViewChanged;
        RelationScrollView.ViewChanged -= OnScrollViewerViewChanged;

        ViewModel?.DataUpdated -= OnDataUpdated;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        UserSpacePageViewModel? oldViewModel,
        UserSpacePageViewModel? newViewModel
    )
    {
        oldViewModel?.DataUpdated -= OnDataUpdated;

        newViewModel?.DataUpdated += OnDataUpdated;
    }

    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        if (sender is ScrollViewer sv && sv.Visibility == Visibility.Visible)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if (
                    sv.ExtentHeight - sv.ViewportHeight - sv.VerticalOffset <= 240
                    && ViewModel is { IsLoading: false, HasMore: true }
                )
                {
                    _ = ViewModel.LoadMoreAsync();
                }
            });
        }
    }

    private async void OnDataUpdated(object? sender, EventArgs e)
    {
        await Task.Delay(300);
        CheckContentFill();
    }

    private void CheckContentFill()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (ViewModel is not { IsLoading: false, HasMore: true })
            {
                return;
            }

            ScrollViewer? activeSv = null;
            bool hasItems = false;

            if (VideoScrollView.Visibility == Visibility.Visible && ViewModel.Videos.Count > 0)
            {
                activeSv = VideoScrollView;
                hasItems = true;
            }
            else if (
                ReviewScrollView.Visibility == Visibility.Visible
                && ViewModel.Reviews.Count > 0
            )
            {
                activeSv = ReviewScrollView;
                hasItems = true;
            }
            else if (
                CircleScrollView.Visibility == Visibility.Visible
                && ViewModel.CircleFeeds.Count > 0
            )
            {
                activeSv = CircleScrollView;
                hasItems = true;
            }
            else if (
                RelationScrollView.Visibility == Visibility.Visible
                && ViewModel.Relations.Count > 0
            )
            {
                activeSv = RelationScrollView;
                hasItems = true;
            }

            if (activeSv != null && hasItems && activeSv.ScrollableHeight <= 240)
            {
                _ = ViewModel.LoadMoreAsync();
            }
        });
    }

    private void VideoItemsRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, VideoRevealFocusPanel);

            CompositionFactory.DisableStandardReposition(element);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void VideoItemsRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            UserVideoItemTemplate.ClearImage(element);
        }
    }

    private void ReviewItemsRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, ReviewRevealFocusPanel);
        }
    }

    private void ReviewItemsRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            UserReviewItemTemplate.ClearImage(element);
        }
    }

    private void CircleItemsRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, CircleRevealFocusPanel);

            CompositionFactory.DisableStandardReposition(element);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void CircleItemsRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
            CircleFeedItemTemplate.ClearImage(element);
        }
    }

    private void RelationItemsRepeater_ElementPrepared(
        ItemsRepeater sender,
        ItemsRepeaterElementPreparedEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            RevealFocusPanel.SetAttachToPanel(element, RelationRevealFocusPanel);

            CompositionFactory.DisableStandardReposition(element);
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                CompositionFactory.SetStandardReposition(element);
            });
        }
    }

    private void RelationItemsRepeater_ElementClearing(
        ItemsRepeater sender,
        ItemsRepeaterElementClearingEventArgs args
    )
    {
        if (args.Element is FrameworkElement element)
        {
            CompositionFactory.DisableStandardReposition(element);
        }
    }
}
