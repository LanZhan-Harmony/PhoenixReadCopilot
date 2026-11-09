using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 设置页面.
/// </summary>
public sealed partial class SettingsPage : SettingsPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPage"/> class.
    /// </summary>
    public SettingsPage() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnPageLoaded()
    {
        ViewModel.InitializeCommand.Execute(null);
        GenericContainer.Visibility = Visibility.Visible;
    }
}

/// <summary>
/// 设置页面基类.
/// </summary>
public abstract class SettingsPageBase : LayoutPageBase<SettingsPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageBase"/> class.
    /// </summary>
    protected SettingsPageBase() =>
        ViewModel = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
}
