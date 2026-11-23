using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 初始登录页面.
/// </summary>
internal sealed partial class StartupPage : StartupPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartupPage"/> class.
    /// </summary>
    public StartupPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded()
    {
        _ = ViewModel.InitializeAsync(QRCodeImage);
    }

    private void OnRefreshQRButtonClick(object sender, RoutedEventArgs e) =>
        _ = ViewModel.ReloadQRCodeAsync();
}

/// <summary>
/// 初始登录页面基类.
/// </summary>
internal abstract class StartupPageBase : LayoutPageBase<StartupPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartupPageBase"/> class.
    /// </summary>
    public StartupPageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<StartupPageViewModel>();
    }
}
