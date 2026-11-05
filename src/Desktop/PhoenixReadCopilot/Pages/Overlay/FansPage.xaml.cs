using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages.Overlay;

/// <summary>
/// 粉丝页面.
/// </summary>
public sealed partial class FansPage : FansPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FansPage"/> class.
    /// </summary>
    public FansPage() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnPageLoaded() => ViewModel.InitializeCommand.Execute(default);
}

/// <summary>
/// 粉丝页面基类.
/// </summary>
public abstract class FansPageBase : LayoutPageBase<FansPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FansPageBase"/> class.
    /// </summary>
    protected FansPageBase() => ViewModel = Ioc.Default.GetRequiredService<FansPageViewModel>();
}
