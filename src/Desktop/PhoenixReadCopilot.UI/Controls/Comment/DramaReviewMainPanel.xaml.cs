using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.UI.ViewModels.Components;
using Richasy.BiliKernel.Models;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Comment;

/// <summary>
/// 剧评主面板.
/// </summary>
public sealed partial class DramaReviewMainPanel : DramaReviewMainPanelBase
{
    public DramaReviewMainPanel() => InitializeComponent();

    private void OnSortChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || ViewModel is null)
        {
            return;
        }

        if (SortTypeComboBox.SelectedItem is CommentSortType sortType && sortType != ViewModel.SortType)
        {
            ViewModel.ChangeSortTypeCommand.Execute(sortType);
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
