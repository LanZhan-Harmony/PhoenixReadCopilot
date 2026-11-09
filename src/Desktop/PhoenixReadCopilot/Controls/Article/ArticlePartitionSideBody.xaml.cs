using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Article;

/// <summary>
/// 文章分区侧边栏.
/// </summary>
public sealed partial class ArticlePartitionSideBody : ArticlePartitionSideBodyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticlePartitionSideBody"/> class.
    /// </summary>
    public ArticlePartitionSideBody() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ViewModel.PartitionInitialized += OnPartitionInitialized;
        CheckPartitionSelection();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        PartitionView.ItemsSource = null;
        ViewModel.PartitionInitialized -= OnPartitionInitialized;
    }

    private void OnPartitionInitialized(object? sender, EventArgs e) => CheckPartitionSelection();

    private void OnPartitionSelectionChanged(
        ItemsView sender,
        ItemsViewSelectionChangedEventArgs args
    )
    {
        PartitionViewModel? item = sender.SelectedItem as PartitionViewModel;
        ViewModel.SelectPartitionCommand.Execute(item);
    }

    private void CheckPartitionSelection()
    {
        if (ViewModel.SelectedPartition is not null)
        {
            PartitionViewModel? partition = ViewModel.Partitions.FirstOrDefault(p =>
                p.Equals(ViewModel.SelectedPartition.Data)
            );
            PartitionView.Select(ViewModel.Partitions.IndexOf(partition));
        }
    }
}

/// <summary>
/// 文章分区侧边栏基类.
/// </summary>
public abstract class ArticlePartitionSideBodyBase
    : LayoutUserControlBase<ArticlePartitionPageViewModel> { }
