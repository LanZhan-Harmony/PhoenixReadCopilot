using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 初始登录页面.
/// </summary>
internal sealed partial class StartupPage : Page
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public StartupPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<StartupPageViewModel>();

    public StartupPage()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.InitializeAsync(QRCodeImage);
    }

    private void OnRefreshQRButtonClick(object sender, RoutedEventArgs e) =>
        _ = ViewModel.ReloadQRCodeAsync();
}
