using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.Controls.Article;

/// <summary>
/// 文章分区头部.
/// </summary>
public sealed partial class ArticlePartitionMainHeader : ArticlePartitionDetailControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticlePartitionMainHeader"/> class.
    /// </summary>
    public ArticlePartitionMainHeader() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        Selector.SelectionChanged += OnSelectorChanged;
        if (ViewModel is null)
        {
            return;
        }

        InitializeChildPartitions();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        ViewModel?.Initialized -= OnViewModelInitialized;

        Selector.SelectionChanged -= OnSelectorChanged;
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        ArticlePartitionDetailViewModel? oldValue,
        ArticlePartitionDetailViewModel? newValue
    )
    {
        oldValue?.Initialized -= OnViewModelInitialized;

        if (newValue is null)
        {
            return;
        }

        newValue.Initialized += OnViewModelInitialized;
        InitializeChildPartitions();
    }

    private void OnViewModelInitialized(object? sender, EventArgs e) => InitializeChildPartitions();

    private void OnSortTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (
            e.AddedItems.Any()
            && e.AddedItems[0] is ArticleSortType sortType
            && sortType != ViewModel.SelectedSortType
        )
        {
            ViewModel.ChangeSortTypeCommand.Execute(sortType);
        }
    }

    private void OnSelectorChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem is null)
        {
            return;
        }

        PartitionViewModel? item = sender.SelectedItem.Tag as PartitionViewModel;
        if (item is not null && item != ViewModel.CurrentPartition)
        {
            ViewModel.ChangeChildPartitionCommand.Execute(item);
        }
    }

    private void InitializeChildPartitions()
    {
        Selector.Items.Clear();
        if (ViewModel.Children is not null)
        {
            foreach (PartitionViewModel item in ViewModel.Children)
            {
                Selector.Items.Add(
                    new SelectorBarItem() { Text = item.Title ?? null, Tag = item }
                );
            }
        }

        Selector.SelectedItem = ViewModel.IsRecommend
            ? Selector.Items.FirstOrDefault()
            : Selector.Items.FirstOrDefault(p => p.Tag == ViewModel.CurrentPartition);
    }
}
