using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频信息区域.
/// </summary>
internal sealed partial class VideoInfoSection : UserControl
{
    private readonly SearchBoxViewModel _searchBoxViewModel =
        Ioc.Default.GetRequiredService<SearchBoxViewModel>();

    public VideoPlayerPageViewModel? ViewModel
    {
        get => (VideoPlayerPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(VideoPlayerPageViewModel),
        typeof(VideoInfoSection),
        new PropertyMetadata(null)
    );

    public VideoInfoSection()
    {
        InitializeComponent();
    }

    private void OnTagButtonClick(object sender, RoutedEventArgs e)
    {
        string? data = (sender as FrameworkElement).DataContext as string;
        _searchBoxViewModel.Search(data);
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
            vm.ShowUserSpace();
        }
    }
}
