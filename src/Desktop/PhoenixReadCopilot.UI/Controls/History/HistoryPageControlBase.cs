using PhoenixReadCopilot.UI.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.History;

/// <summary>
/// 历史记录页头部基类.
/// </summary>
public abstract class HistoryPageControlBase : LayoutUserControlBase<HistoryPageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryPageControlBase"/> class.
    /// </summary>
    protected HistoryPageControlBase() => ViewModel = this.Get<HistoryPageViewModel>();
}
