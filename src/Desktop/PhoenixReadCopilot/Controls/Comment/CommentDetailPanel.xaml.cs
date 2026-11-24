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

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentDetailPanel"/> class.
    /// </summary>
    public CommentDetailPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private void OnControlLoaded()
    {
        CommentScrollView.ViewChanged += OnViewChanged;
        CommentScrollView.SizeChanged += OnScrollViewSizeChanged;
        if (ViewModel is null)
        {
            return;
        }

        CheckCommentCount();
    }

    private void OnControlUnloaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.ListUpdated -= OnCommentListUpdatedAsync;
        }

        CommentScrollView.ViewChanged -= OnViewChanged;
        CommentScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private void OnViewModelChanged(
        CommentDetailViewModel? oldValue,
        CommentDetailViewModel? newValue
    )
    {
        if (oldValue is not null)
        {
            oldValue.ListUpdated -= OnCommentListUpdatedAsync;
        }

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
                CommentScrollView.ExtentHeight
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
