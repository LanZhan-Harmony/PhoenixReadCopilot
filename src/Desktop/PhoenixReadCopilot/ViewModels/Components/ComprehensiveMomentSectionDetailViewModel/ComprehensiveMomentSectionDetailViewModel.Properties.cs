using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;
using Richasy.BiliKernel.Bili.Moment;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 综合动态分区详情视图模型.
/// </summary>
public sealed partial class ComprehensiveMomentSectionDetailViewModel
{
    private readonly IMomentDiscoveryService _service;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly MomentPageViewModel _momentPageViewModel;
    private readonly ILogger<ComprehensiveMomentSectionDetailViewModel> _logger;
    private readonly ILogger<MomentUperSectionViewModel> _uperLogger;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial MomentUperSectionViewModel? SelectedUper { get; set; }

    /// <summary>
    /// 已经初始化完成.
    /// </summary>
    public event EventHandler Initialized;

    /// <inheritdoc/>
    public MomentSectionType SectionType => MomentSectionType.Comprehensive;

    /// <summary>
    /// 推荐的用户列表.
    /// </summary>
    public ObservableCollection<MomentUperSectionViewModel> Upers { get; } = [];
}
