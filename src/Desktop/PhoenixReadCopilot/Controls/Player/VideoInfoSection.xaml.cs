using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频信息区域.
/// </summary>
public sealed partial class VideoInfoSection : VideoPlayerPageControlBase
{
    private readonly SearchBoxViewModel _searchBoxViewModel =
        Ioc.Default.GetRequiredService<SearchBoxViewModel>();

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoInfoSection"/> class.
    /// </summary>
    public VideoInfoSection()
    {
        InitializeComponent();
    }

    private void OnTagButtonClick(object sender, RoutedEventArgs e)
    {
        string? data = (sender as FrameworkElement).DataContext as string;
        _searchBoxViewModel.SearchCommand.Execute(data);
    }

    private void OnLoopSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null || LoopComboBox.SelectedItem is not VideoLoopType loopType)
        {
            return;
        }

        if (ViewModel.CurrentLoop != loopType)
        {
            ViewModel.CurrentLoop = loopType;
        }
    }

    private void OnCreditItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DramaCreditItemViewModel vm)
        {
            vm.ShowUserSpaceCommand.Execute(null);
        }
    }
}
