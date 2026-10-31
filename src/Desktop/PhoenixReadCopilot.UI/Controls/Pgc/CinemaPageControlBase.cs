using PhoenixReadCopilot.UI.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Pgc;

/// <summary>
/// 影院页面控件基类.
/// </summary>
public abstract class CinemaPageControlBase : LayoutUserControlBase<CinemaPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CinemaPageControlBase"/> class.
    /// </summary>
    protected CinemaPageControlBase() => ViewModel = this.Get<CinemaPageViewModel>();
}
