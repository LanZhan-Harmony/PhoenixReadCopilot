using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 动态页面.
/// </summary>
public sealed partial class MomentPage : MomentPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentPage"/> class.
    /// </summary>
    public MomentPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded() => ViewModel.InitializeCommand.Execute(null);
}

/// <summary>
/// 动态页面基类.
/// </summary>
public abstract class MomentPageBase : LayoutPageBase<MomentPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentPageBase"/> class.
    /// </summary>
    protected MomentPageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<MomentPageViewModel>();
    }
}
