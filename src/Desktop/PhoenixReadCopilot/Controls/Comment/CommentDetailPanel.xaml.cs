using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Comment;

/// <summary>
/// 评论详情面板.
/// </summary>
internal sealed partial class CommentDetailPanel : UserControl
{
    public CommentDetailViewModel? ViewModel
    {
        get => (CommentDetailViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(CommentDetailViewModel),
        typeof(CommentDetailPanel),
        new PropertyMetadata(null, (d, e) =>
            ((CommentDetailPanel)d).OnViewModelChanged(
                (CommentDetailViewModel?)e.OldValue,
                (CommentDetailViewModel?)e.NewValue
            )
        )
    );

    public CommentDetailPanel()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        CheckCommentCount();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel?.ListUpdated -= OnCommentListUpdatedAsync;
    }

    private void OnViewModelChanged(
        CommentDetailViewModel? oldValue,
        CommentDetailViewModel? newValue
    )
    {
        oldValue?.ListUpdated -= OnCommentListUpdatedAsync;

        if (newValue is null)
        {
            return;
        }

        newValue.ListUpdated += OnCommentListUpdatedAsync;
    }

    private async void OnCommentListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckCommentCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                ViewModel is not null
                && CommentScrollView.ExtentHeight
                    - CommentScrollView.ViewportHeight
                    - CommentScrollView.VerticalOffset
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
            CheckCommentCount();
        }
    }

    private void CheckCommentCount()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (CommentScrollView.ScrollableHeight <= 240 && ViewModel is not null)
            {
                _ = ViewModel.LoadItemsAsync();
            }
        });
    }
}
