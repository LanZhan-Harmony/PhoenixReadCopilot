using PhoenixReadCopilot.UI.ViewModels.View;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Message;

/// <summary>
/// 消息页面控件基类.
/// </summary>
public abstract class MessagePageControlBase : LayoutUserControlBase<MessagePageViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePageControlBase"/> class.
    /// </summary>
    protected MessagePageControlBase() => ViewModel = this.Get<MessagePageViewModel>();
}
