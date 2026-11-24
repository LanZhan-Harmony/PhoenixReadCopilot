using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Search;

/// <summary>
/// 搜索侧边栏.
/// </summary>
internal sealed partial class SearchSideBody : UserControl
{
    public SearchPageViewModel? ViewModel
    {
        get => (SearchPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(SearchPageViewModel),
        typeof(SearchSideBody),
        new PropertyMetadata(null)
    );

    public SearchSideBody()
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
        ISearchSectionDetailViewModel? item = sender.SelectedItem as ISearchSectionDetailViewModel;
        if (item is not null && ViewModel is not null)
        {
            ViewModel.SelectSection(item);
        }
    }

    private void CheckSectionSelection()
    {
        if (ViewModel?.SelectedSection is not null)
        {
            SectionView.Select(ViewModel.Sections.ToList().IndexOf(ViewModel.SelectedSection));
        }
    }
}
