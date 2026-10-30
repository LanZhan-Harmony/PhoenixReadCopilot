using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Moment;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 动态页面视图模型.
/// </summary>
public sealed partial class MomentPageViewModel
{
    private readonly IMomentDiscoveryService _momentDiscoveryService;

    [ObservableProperty]
    public partial List<IMomentSectionDetailViewModel> Sections { get; set; }

    [ObservableProperty]
    public partial IMomentSectionDetailViewModel SelectedSection { get; set; }

    [ObservableProperty]
    public partial bool IsCommentsOpened { get; set; }

    /// <summary>
    /// 区块初始化完成.
    /// </summary>
    public event EventHandler Initialized;

    /// <summary>
    /// 评论模块.
    /// </summary>
    public CommentMainViewModel CommentModule { get; }
}
