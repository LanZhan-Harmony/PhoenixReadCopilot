using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.UI.ViewModels.Components;
using Richasy.BiliKernel.Models.Base;

namespace PhoenixReadCopilot.UI.Controls.Player;

/// <summary>
/// 视频描述组件.
/// </summary>
public sealed partial class VideoDescriptorControl : VideoPlayerPageControlBase
{
    private readonly SearchBoxViewModel _searchBoxViewModel = Ioc.Default.GetRequiredService<SearchBoxViewModel>();
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoDescriptorControl"/> class.
    /// </summary>
    public VideoDescriptorControl() => InitializeComponent();

    protected override void OnControlUnloaded() => TagRepeater.ItemsSource = null;

    private void OnTagButtonClick(object sender, RoutedEventArgs e)
    {
        var data = (sender as FrameworkElement).DataContext as BiliTag;
        _searchBoxViewModel.SearchCommand.Execute(data.Name);
    }
}
