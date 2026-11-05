using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 动漫页面视图模型.
/// </summary>
public sealed partial class AnimePageViewModel
{
    private readonly IEntertainmentDiscoveryService _service;
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
    public event EventHandler SectionInitialized;
}
