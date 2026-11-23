using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 位于标题栏的账户控件.
/// </summary>
internal sealed partial class HeaderAccountControl : HeaderAccountControlBase
{
    private readonly IWindowService _windowService = Ioc.Default.GetRequiredService<IWindowService>();

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderAccountControl"/> class.
    /// </summary>
    public HeaderAccountControl()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded() => _ = ViewModel.InitializeAsync();

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

/// <summary>
/// 位于标题栏的账户控件基类.
/// </summary>
internal abstract class HeaderAccountControlBase : LayoutUserControlBase<AccountViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderAccountControlBase"/> class.
    /// </summary>
    protected HeaderAccountControlBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<AccountViewModel>();
    }
}
