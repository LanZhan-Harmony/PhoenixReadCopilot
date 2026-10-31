using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.Controls.History;

/// <summary>
/// 历史记录页头部.
/// </summary>
public sealed partial class HistoryHeader : HistoryPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryHeader"/> class.
    /// </summary>
    public HistoryHeader() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        Selector.SelectionChanged += OnSelectorChanged;
        ViewModel.SectionInitialized += OnViewModelSectionInitialized;
        InitializeChildPartitions();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.SectionInitialized -= OnViewModelSectionInitialized;
        }

        Selector.SelectionChanged -= OnSelectorChanged;
    }

    private static string GetSectionName(ViewHistoryTabType type)
    {
        return type switch
        {
            ViewHistoryTabType.Video => Strings.Resources.Video,
            ViewHistoryTabType.Article => Strings.Resources.Article,
            _ => string.Empty,
        };
    }

    private void OnViewModelSectionInitialized(object? sender, EventArgs e) =>
        InitializeChildPartitions();

    private void OnSelectorChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (sender.SelectedItem is null)
        {
            return;
        }

        var item = sender.SelectedItem.Tag as IHistorySectionDetailViewModel;
        if (item is not null)
        {
            ViewModel.SelectSectionCommand.Execute(item);
        }
    }

    private void InitializeChildPartitions()
    {
        Selector.Items.Clear();
        if (ViewModel.Sections is not null)
        {
            foreach (var item in ViewModel.Sections)
            {
                Selector.Items.Add(
                    new SelectorBarItem() { Text = GetSectionName(item.Type), Tag = item }
                );
            }
        }

        Selector.SelectedItem = Selector.Items.FirstOrDefault(p =>
            p.Tag == ViewModel.SelectedSection
        );
    }

    private void OnCleanButtonClick(object sender, RoutedEventArgs e) => CleanTip.IsOpen = true;
}
