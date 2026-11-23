using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Rank;

/// <summary>
/// 榜单区块项控件.
/// </summary>
internal sealed partial class RankSectionItemControl : RankSectionItemControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RankSectionItemControl"/> class.
    /// </summary>
    public RankSectionItemControl()
    {
        InitializeComponent();
    }
}

/// <summary>
/// 榜单区块项控件基类.
/// </summary>
internal abstract class RankSectionItemControlBase
    : LayoutUserControlBase<RankSectionItemViewModel> { }
