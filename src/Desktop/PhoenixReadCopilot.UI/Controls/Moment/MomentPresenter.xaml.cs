using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Moment;

/// <summary>
/// 动态显示器.
/// </summary>
public sealed partial class MomentPresenter : MomentPresenterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentPresenter"/> class.
    /// </summary>
    public MomentPresenter() => InitializeComponent();
}

/// <summary>
/// 动态显示器基类.
/// </summary>
public abstract class MomentPresenterBase : LayoutUserControlBase<MomentItemViewModel> { }
