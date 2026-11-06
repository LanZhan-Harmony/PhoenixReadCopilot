using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Components;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Comment;

/// <summary>
/// 剧评主面板.
/// </summary>
public sealed partial class DramaReviewMainPanel : DramaReviewMainPanelBase
{
    public DramaReviewMainPanel() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ReviewScrollView.ViewChanged += OnViewChanged;
        ReviewScrollView.SizeChanged += OnScrollViewSizeChanged;
        if (ViewModel is null)
        {
            return;
        }

        CheckSortType();
        CheckReviewCount();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.ListUpdated -= OnReviewListUpdatedAsync;
            ViewModel.Initialized -= OnInitialized;
        }

        ReviewScrollView.ViewChanged -= OnViewChanged;
        ReviewScrollView.SizeChanged -= OnScrollViewSizeChanged;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
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
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (
                    ReviewScrollView.ExtentHeight
                    - ReviewScrollView.ViewportHeight
                    - ReviewScrollView.VerticalOffset
                    <= 240
                )
                {
                    if (ViewModel is not null && ViewModel.HasMore && !ViewModel.IsLoading)
                    {
                        ViewModel.LoadMoreCommand.Execute(default);
                    }
                }
            }
        );
    }

    private void OnInitialized(object? sender, EventArgs e)
    {
        CheckSortType();
        CheckReviewCount();
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
        DispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                if (ReviewScrollView.ScrollableHeight <= 240 && ViewModel is not null && ViewModel.HasMore && !ViewModel.IsLoading)
                {
                    ViewModel.LoadMoreCommand.Execute(default);
                }
            }
        );
    }

    private void CheckSortType()
    {
        if (ViewModel is null)
        {
            return;
        }

        var index = ViewModel.Sorts.IndexOf(ViewModel.SortType);
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

        if (SortTypeComboBox.SelectedItem is CommentSortOrder sortOrder && sortOrder != ViewModel.SortType)
        {
            ViewModel.ChangeSortTypeCommand.Execute(sortOrder);
        }
    }

    private async void OnSendReviewClick(object sender, RoutedEventArgs e)
    {
        var text = ReviewInputBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) || ViewModel is null)
        {
            return;
        }

        await ViewModel.SendReviewCommand.ExecuteAsync(text);
        ReviewInputBox.Text = string.Empty;
    }
}

/// <summary>
/// 剧评主面板基类.
/// </summary>
public abstract class DramaReviewMainPanelBase : LayoutUserControlBase<DramaReviewSectionDetailViewModel> { }
