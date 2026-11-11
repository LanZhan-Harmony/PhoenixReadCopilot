using CommunityToolkit.Mvvm.DependencyInjection;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Pages.Overlay;

/// <summary>
/// 关注页面.
/// </summary>
public sealed partial class FollowsPage : FollowsPageBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FollowsPage"/> class.
    /// </summary>
    public FollowsPage()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnPageLoaded() => ViewModel.InitializeCommand.Execute(null);
}

/// <summary>
/// 关注页面基类.
/// </summary>
public abstract class FollowsPageBase : LayoutPageBase<FollowsPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FollowsPageBase"/> class.
    /// </summary>
    protected FollowsPageBase()
    {
        ViewModel = Ioc.Default.GetRequiredService<FollowsPageViewModel>();
    }
}
