using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 榜单页.
/// </summary>
public sealed partial class RankPage : RankPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RankPage"/> class.
    /// </summary>
    public RankPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded() => _ = ViewModel.InitializeAsync();
}

/// <summary>
/// 榜单页面基类.
/// </summary>
public abstract class RankPageBase : LayoutPageBase<RankPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RankPageBase"/> class.
    /// </summary>
    protected RankPageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<RankPageViewModel>();
    }
}
