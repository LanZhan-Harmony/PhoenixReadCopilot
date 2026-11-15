using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Rank;

/// <summary>
/// 榜单页面侧边栏主体.
/// </summary>
public sealed partial class RankSideBody : RankPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RankSideBody"/> class.
    /// </summary>
    public RankSideBody()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ViewModel.SectionInitialized += OnSectionInitialized;
        SectionView.SelectionChanged += OnSectionSelectionChanged;
        CheckSectionSelection();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        SectionView.ItemsSource = null;
        ViewModel.SectionInitialized -= OnSectionInitialized;
        SectionView.SelectionChanged -= OnSectionSelectionChanged;
    }

    private void OnSectionInitialized(object? sender, EventArgs e) => CheckSectionSelection();

    private void OnSectionSelectionChanged(
        ItemsView sender,
        ItemsViewSelectionChangedEventArgs args
    )
    {
        IRankSectionItemViewModel? item = sender.SelectedItem as IRankSectionItemViewModel;
        _ = ViewModel.SelectSectionAsync(item);
    }

    private void CheckSectionSelection()
    {
        if (ViewModel.SelectedSection is not null)
        {
            SectionView.Select(ViewModel.Sections.IndexOf(ViewModel.SelectedSection));
        }
    }
}
