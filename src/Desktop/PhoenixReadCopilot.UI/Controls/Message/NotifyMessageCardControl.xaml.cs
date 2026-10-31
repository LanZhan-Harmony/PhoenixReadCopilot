using PhoenixReadCopilot.UI.ViewModels.Items;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Message;

/// <summary>
/// 通知消息卡片控件.
/// </summary>
public sealed partial class NotifyMessageCardControl : NotifyMessageCardControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyMessageCardControl"/> class.
    /// </summary>
    public NotifyMessageCardControl()
    {
        InitializeComponent();
    }
}

/// <summary>
/// 通知消息卡片控件.
/// </summary>
public abstract class NotifyMessageCardControlBase
    : LayoutUserControlBase<NotifyMessageItemViewModel>;
