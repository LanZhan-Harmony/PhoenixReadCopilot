using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Popular;

/// <summary>
/// 演员排行榜单卡片控件.
/// </summary>
public sealed partial class ActorRankCardControl : ActorRankCardControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActorRankCardControl"/> class.
    /// </summary>
    public ActorRankCardControl()
    {
        InitializeComponent();
    }
}

/// <summary>
/// 演员排行榜单卡片控件基类.
/// </summary>
public abstract class ActorRankCardControlBase
    : LayoutUserControlBase<ActorItemViewModel>;
