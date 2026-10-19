using Microsoft.UI.Xaml.Controls.Primitives;
using PhoenixReadCopilot.UI.ViewModels.View;
using Richasy.WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Pages;

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
        ViewModel.InitializeCommand.Execute(default);
        GenericContainer.Visibility = Visibility.Visible;
    }

    private void OnJoinGroupButtonClick(object sender, RoutedEventArgs e) =>
        FlyoutBase.ShowAttachedFlyout(sender as FrameworkElement);
}

/// <summary>
/// 设置页面基类.
/// </summary>
public abstract class SettingsPageBase : LayoutPageBase<SettingsPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageBase"/> class.
    /// </summary>
    protected SettingsPageBase() => ViewModel = this.Get<SettingsPageViewModel>();
}
