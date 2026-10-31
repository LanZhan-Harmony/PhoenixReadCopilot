using PhoenixReadCopilot.UI.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Users;

/// <summary>
/// 关注页面控件基类.
/// </summary>
public abstract class FollowsPageControlBase : LayoutUserControlBase<FollowsPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FollowsPageControlBase"/> class.
    /// </summary>
    protected FollowsPageControlBase() => ViewModel = this.Get<FollowsPageViewModel>();
}
