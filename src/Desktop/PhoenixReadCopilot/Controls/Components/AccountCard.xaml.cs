using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 账户卡片.
/// </summary>
internal sealed partial class AccountCard : UserControl
{
    public AccountViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<AccountViewModel>();

    public AccountCard()
    {
        InitializeComponent();
    }
}
