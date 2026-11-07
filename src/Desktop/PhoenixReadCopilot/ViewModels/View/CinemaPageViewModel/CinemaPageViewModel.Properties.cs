using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 找剧 / 多维筛选页面视图模型 (原影院页面).
/// </summary>
public sealed partial class CinemaPageViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<CinemaPageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILoggerFactory _loggerFactory;

    [ObservableProperty]
    public partial List<EntertainmentIndexViewModel>? Sections { get; set; }

    [ObservableProperty]
    public partial EntertainmentIndexViewModel? SelectedSection { get; set; }

    /// <summary>
    /// 区块加载完成.
    /// </summary>
    public event EventHandler? SectionInitialized;
}
