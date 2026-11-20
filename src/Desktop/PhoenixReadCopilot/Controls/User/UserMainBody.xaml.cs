using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

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

        if (ViewModel != null)
        {
            ViewModel.DataUpdated += OnDataUpdated;
        }
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        VideoScrollView.ViewChanged -= OnScrollViewerViewChanged;
        ReviewScrollView.ViewChanged -= OnScrollViewerViewChanged;
        CircleScrollView.ViewChanged -= OnScrollViewerViewChanged;
        RelationScrollView.ViewChanged -= OnScrollViewerViewChanged;

        if (ViewModel != null)
        {
            ViewModel.DataUpdated -= OnDataUpdated;
        }
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(UserSpacePageViewModel? oldViewModel, UserSpacePageViewModel? newViewModel)
    {
        if (oldViewModel != null)
        {
            oldViewModel.DataUpdated -= OnDataUpdated;
        }

        if (newViewModel != null)
        {
            newViewModel.DataUpdated += OnDataUpdated;
        }
    }

    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        if (sender is ScrollViewer sv && sv.Visibility == Visibility.Visible)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if (sv.ExtentHeight - sv.ViewportHeight - sv.VerticalOffset <= 240
                    && ViewModel is { IsLoading: false, HasMore: true })
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
            else if (ReviewScrollView.Visibility == Visibility.Visible && ViewModel.Reviews.Count > 0)
            {
                activeSv = ReviewScrollView;
                hasItems = true;
            }
            else if (CircleScrollView.Visibility == Visibility.Visible && ViewModel.CircleFeeds.Count > 0)
            {
                activeSv = CircleScrollView;
                hasItems = true;
            }
            else if (RelationScrollView.Visibility == Visibility.Visible && ViewModel.Relations.Count > 0)
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
}
