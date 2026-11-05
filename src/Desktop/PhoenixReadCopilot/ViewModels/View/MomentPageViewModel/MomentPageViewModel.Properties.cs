using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Moment;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 动态页面视图模型.
/// </summary>
public sealed partial class MomentPageViewModel
{
    private readonly IMomentDiscoveryService _momentDiscoveryService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISettingsService _settingsService;

    private AppSettings _settings => _settingsService.Settings;

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
