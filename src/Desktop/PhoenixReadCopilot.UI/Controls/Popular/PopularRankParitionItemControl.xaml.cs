using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Popular;

/// <summary>
/// 流行视频排行榜分区项控件.
/// </summary>
public sealed partial class PopularRankParitionItemControl : PopularRankParitionItemControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PopularRankParitionItemControl"/> class.
    /// </summary>
    public PopularRankParitionItemControl() => InitializeComponent();
}

/// <summary>
/// 流行视频排行榜分区项控件基类.
/// </summary>
public abstract class PopularRankParitionItemControlBase
    : LayoutUserControlBase<PopularRankPartitionViewModel> { }
