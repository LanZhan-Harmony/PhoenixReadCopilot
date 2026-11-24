using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 影院页面.
/// </summary>
internal sealed partial class FilterPage : Page
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public FilterPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<FilterPageViewModel>();

    public FilterPage()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = ViewModel.InitializeAsync();
    }
}
