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

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPage"/> class.
    /// </summary>
    public FilterPage()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = ViewModel.InitializeAsync();
    }
}
