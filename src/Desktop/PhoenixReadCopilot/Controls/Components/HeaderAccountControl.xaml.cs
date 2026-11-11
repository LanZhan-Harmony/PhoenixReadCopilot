using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 位于标题栏的账户控件.
/// </summary>
public sealed partial class HeaderAccountControl : HeaderAccountControlBase
{
    private readonly AppViewModel _appViewModel = Ioc.Default.GetRequiredService<AppViewModel>();

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderAccountControl"/> class.
    /// </summary>
    public HeaderAccountControl() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded() => ViewModel.InitializeCommand.Execute(null);

    private void OnFlyoutOpened(object sender, object e) =>
        ViewModel.UpdateCommunityInformationCommand.Execute(null);

    private void OnAvatarButtonClick(object sender, RoutedEventArgs e)
    {
        AccountFlyout.ShowAt(AvatarButton);
    }

    private void OnSignInFlyoutItemClick(object sender, RoutedEventArgs e)
    {
        Window? window = _appViewModel.Windows.FirstOrDefault(p => p is StartupWindow);
        if (window is not null)
        {
            try
            {
                window.Activate();
                return;
            }
            catch
            {
                _appViewModel.Windows.Remove(window);
            }
        }

        new StartupWindow().Activate();
    }
}

/// <summary>
/// 位于标题栏的账户控件基类.
/// </summary>
public abstract class HeaderAccountControlBase : LayoutUserControlBase<AccountViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderAccountControlBase"/> class.
    /// </summary>
    protected HeaderAccountControlBase() =>
        ViewModel = Ioc.Default.GetRequiredService<AccountViewModel>();
}
