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

    [ObservableProperty] private List<IMomentSectionDetailViewModel> _sections;

    [ObservableProperty] private IMomentSectionDetailViewModel _selectedSection;

    [ObservableProperty] private bool _isCommentsOpened;

    /// <summary>
    /// 区块初始化完成.
    /// </summary>
    public event EventHandler Initialized;

    /// <summary>
    /// 评论模块.
    /// </summary>
    public CommentMainViewModel CommentModule { get; }
}
