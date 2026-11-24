using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 榜单页.
/// </summary>
internal sealed partial class RankPage : Page
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public RankPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<RankPageViewModel>();

    public RankPage()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = ViewModel.InitializeAsync();
    }
}
