using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 上新日历页面视图模型 (原动漫页面).
/// </summary>
public sealed partial class AnimePageViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<AnimePageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILoggerFactory _loggerFactory;

    [ObservableProperty]
    public partial List<IPgcSectionDetailViewModel>? Sections { get; set; }

    [ObservableProperty]
    public partial IPgcSectionDetailViewModel? SelectedSection { get; set; }

    /// <summary>
    /// 区块加载完成.
    /// </summary>
    public event EventHandler? SectionInitialized;
}
