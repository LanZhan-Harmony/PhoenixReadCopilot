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

    /// <summary>
    /// Initializes a new instance of the <see cref="RankPage"/> class.
    /// </summary>
    public RankPage()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = ViewModel.InitializeAsync();
    }
}
