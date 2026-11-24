using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 位于标题栏的账户控件.
/// </summary>
internal sealed partial class HeaderAccountControl : UserControl
{
    private readonly IWindowService _windowService = Ioc.Default.GetRequiredService<IWindowService>();

    public AccountViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<AccountViewModel>();

    public HeaderAccountControl()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = ViewModel.InitializeAsync();
    }

    private void OnFlyoutOpened(object sender, object e) =>
        _ = ViewModel.UpdateCommunityInformationAsync();

    private void OnAvatarButtonClick(object sender, RoutedEventArgs e)
    {
        AccountFlyout.ShowAt(AvatarButton);
    }

    private void OnSignInFlyoutItemClick(object sender, RoutedEventArgs e)
    {
        Window? window = _windowService.GetWindow<StartupWindow>();
        if (window is not null)
        {
            try
            {
                window.Activate();
                return;
            }
            catch
            {
                _windowService.UnregisterWindow(window);
            }
        }

        new StartupWindow().Activate();
    }
}
