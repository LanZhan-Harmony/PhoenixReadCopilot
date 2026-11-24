using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Comment;

/// <summary>
/// 剧评主面板.
/// </summary>
internal sealed partial class DramaReviewMainPanel : UserControl
{
    public DramaReviewSectionDetailViewModel? ViewModel
    {
        get => (DramaReviewSectionDetailViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(DramaReviewSectionDetailViewModel),
        typeof(DramaReviewMainPanel),
        new PropertyMetadata(null, (d, e) =>
            ((DramaReviewMainPanel)d).OnViewModelChanged(
                (DramaReviewSectionDetailViewModel?)e.OldValue,
                (DramaReviewSectionDetailViewModel?)e.NewValue
            )
        )
    );

    public DramaReviewMainPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private void OnControlLoaded()
    {
        ReviewScrollView.ViewChanged += OnViewChanged;
        ReviewScrollView.SizeChanged += OnScrollViewSizeChanged;
        ReviewCommentBox?.UpdateLoginState();
        if (ViewModel is null)
        {
            return;
        }

        CheckSortType();
        CheckReviewCount();
    }

    private void OnControlUnloaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.ListUpdated -= OnReviewListUpdatedAsync;
            ViewModel.Initialized -= OnInitialized;
        }

        ReviewScrollView.ViewChanged -= OnViewChanged;
        ReviewScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    private void OnViewModelChanged(
        DramaReviewSectionDetailViewModel? oldValue,
        DramaReviewSectionDetailViewModel? newValue
    )
    {
        if (oldValue is not null)
        {
            oldValue.ListUpdated -= OnReviewListUpdatedAsync;
            oldValue.Initialized -= OnInitialized;
        }

        if (newValue is null)
        {
            return;
        }

        newValue.Initialized += OnInitialized;
        newValue.ListUpdated += OnReviewListUpdatedAsync;
    }

    private async void OnReviewListUpdatedAsync(object? sender, EventArgs e)
    {
        await Task.Delay(500);
        CheckReviewCount();
    }

    private void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                ReviewScrollView.ExtentHeight
                    - ReviewScrollView.ViewportHeight
                    - ReviewScrollView.VerticalOffset
                <= 240
            )
            {
                if (ViewModel is not null && ViewModel is { HasMore: true, IsLoading: false })
                {
                    _ = ViewModel.LoadMoreAsync();
                }
            }
        });
    }

    private void OnInitialized(object? sender, EventArgs e)
    {
        CheckSortType();
        CheckReviewCount();
        ReviewCommentBox?.UpdateLoginState();
    }

    private void OnScrollViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 100)
        {
            CheckReviewCount();
        }
    }

    private void CheckReviewCount()
    {
        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            if (
                ReviewScrollView.ScrollableHeight <= 240
                && ViewModel is not null
                && ViewModel is { HasMore: true, IsLoading: false }
            )
            {
                _ = ViewModel.LoadMoreAsync();
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
        if (!IsLoaded || ViewModel is null)
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
