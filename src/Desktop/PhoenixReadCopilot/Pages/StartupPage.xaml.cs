using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 初始登录页面.
/// </summary>
public sealed partial class StartupPage : StartupPageBase
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
        ViewModel.InitializeCommand.Execute(QRCodeImage);
    }

    private void OnRefreshQRButtonClick(object sender, RoutedEventArgs e) =>
        ViewModel.ReloadQRCodeCommand.Execute(null);
}

/// <summary>
/// 初始登录页面基类.
/// </summary>
public abstract class StartupPageBase : LayoutPageBase<StartupPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartupPageBase"/> class.
    /// </summary>
    public StartupPageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<StartupPageViewModel>();
    }
}
