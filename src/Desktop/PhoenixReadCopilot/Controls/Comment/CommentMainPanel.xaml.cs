using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Comment;

/// <summary>
/// 评论详情面板.
/// </summary>
internal sealed partial class CommentMainPanel : UserControl
{
    public CommentMainViewModel? ViewModel
    {
        get => (CommentMainViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(CommentMainViewModel),
        typeof(CommentMainPanel),
        new PropertyMetadata(null, (d, e) =>
            ((CommentMainPanel)d).OnViewModelChanged(
                (CommentMainViewModel?)e.OldValue,
                (CommentMainViewModel?)e.NewValue
            )
        )
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentMainPanel"/> class.
    /// </summary>
    public CommentMainPanel()
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

        CheckSortType();
        CheckCommentCount();
    }

    private void OnControlUnloaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.ListUpdated -= OnCommentListUpdatedAsync;
            ViewModel.Initialized -= OnInitialized;
        }

        CommentScrollView.ViewChanged -= OnViewChanged;
        CommentScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private void OnViewModelChanged(CommentMainViewModel? oldValue, CommentMainViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.ListUpdated -= OnCommentListUpdatedAsync;
            oldValue.Initialized -= OnInitialized;
        }

        if (newValue is null)
        {
            return;
        }

        newValue.Initialized += OnInitialized;
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

    private void OnInitialized(object? sender, EventArgs e)
    {
        CheckSortType();
        CheckCommentCount();
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100)
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

    private void CheckSortType()
    {
        if (ViewModel is null)
        {
            return;
        }

        int index = ViewModel.Sorts.IndexOf(ViewModel.SortType);
        if (index >= 0 && SortTypeComboBox.SelectedIndex != index)
        {
            SortTypeComboBox.SelectedIndex = index;
        }
    }

    private void OnSortChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || SortTypeComboBox.SelectedIndex < 0 || ViewModel is null)
        {
            return;
        }

        if (
            SortTypeComboBox.SelectedItem is CommentSortOrder sortOrder
            && sortOrder != ViewModel.SortType
        )
        {
            _ = ViewModel.ChangeSortTypeAsync(sortOrder);
        }
    }
}
