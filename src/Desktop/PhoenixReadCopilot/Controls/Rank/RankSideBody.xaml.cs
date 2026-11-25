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

    public RankSideBody()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel?.SectionInitialized += OnSectionInitialized;
        CheckSectionSelection();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel?.SectionInitialized -= OnSectionInitialized;
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
