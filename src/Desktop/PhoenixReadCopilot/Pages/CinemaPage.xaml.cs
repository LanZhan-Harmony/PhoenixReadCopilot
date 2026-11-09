using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 影院页面.
/// </summary>
public partial class CinemaPage : CinemaPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CinemaPage"/> class.
    /// </summary>
    public CinemaPage() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnPageLoaded() => ViewModel.InitializeCommand.Execute(null);
}

/// <summary>
/// 影院页面基类.
/// </summary>
public abstract class CinemaPageBase : LayoutPageBase<CinemaPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CinemaPageBase"/> class.
    /// </summary>
    protected CinemaPageBase() => ViewModel = Ioc.Default.GetRequiredService<CinemaPageViewModel>();
}
