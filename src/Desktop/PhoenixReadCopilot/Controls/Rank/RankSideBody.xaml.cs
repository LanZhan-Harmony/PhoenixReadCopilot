using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Rank;

/// <summary>
/// 榜单页面侧边栏主体.
/// </summary>
internal sealed partial class RankSideBody : UserControl
{
    public RankPageViewModel? ViewModel
    {
        get => (RankPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(RankPageViewModel),
        typeof(RankSideBody),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="RankSideBody"/> class.
    /// </summary>
    public RankSideBody()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private void OnControlLoaded()
    {
        if (ViewModel is not null)
        {
            ViewModel.SectionInitialized += OnSectionInitialized;
        }

        SectionView.SelectionChanged += OnSectionSelectionChanged;
        CheckSectionSelection();
    }

    private void OnControlUnloaded()
    {
        SectionView.ItemsSource = null;
        if (ViewModel is not null)
        {
            ViewModel.SectionInitialized -= OnSectionInitialized;
        }

        SectionView.SelectionChanged -= OnSectionSelectionChanged;
    }

    private void OnSectionInitialized(object? sender, EventArgs e) => CheckSectionSelection();

    private void OnSectionSelectionChanged(
        ItemsView sender,
        ItemsViewSelectionChangedEventArgs args
    )
    {
        RankSectionItemViewModel? item = sender.SelectedItem as RankSectionItemViewModel;
        if (item is not null && ViewModel is not null)
        {
            _ = ViewModel.SelectSectionAsync(item);
        }
    }

    private void CheckSectionSelection()
    {
        if (ViewModel?.SelectedSection is null)
        {
            return;
        }

        int index = ViewModel.Sections.IndexOf(ViewModel.SelectedSection);
        if (index >= 0 && SectionView.SelectedItem != ViewModel.SelectedSection)
        {
            SectionView.Select(index);
        }
    }
}
