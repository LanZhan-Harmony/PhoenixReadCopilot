using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.Components;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 位于标题栏的账户控件.
/// </summary>
public sealed partial class HeaderAccountControl : HeaderAccountControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderAccountControl"/> class.
    /// </summary>
    public HeaderAccountControl() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded() => ViewModel.InitializeCommand.Execute(null);

    private void OnFlyoutOpened(object sender, object e) =>
        ViewModel.UpdateCommunityInformationCommand.Execute(null);
}

/// <summary>
/// 位于标题栏的账户控件基类.
/// </summary>
public abstract class HeaderAccountControlBase : LayoutUserControlBase<AccountViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderAccountControlBase"/> class.
    /// </summary>
    protected HeaderAccountControlBase() => ViewModel = Ioc.Default.GetRequiredService<AccountViewModel>();
}
