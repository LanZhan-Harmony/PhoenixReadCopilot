using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Components;
using Richasy.BiliKernel.Bili.Media;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 影院页面视图模型.
/// </summary>
public sealed partial class CinemaPageViewModel
{
    private readonly IEntertainmentDiscoveryService _service;
    private readonly ILogger<CinemaPageViewModel> _logger;

    [ObservableProperty]
    public partial List<EntertainmentIndexViewModel>? Sections { get; set; }

    [ObservableProperty]
    public partial EntertainmentIndexViewModel? SelectedSection { get; set; }

    /// <summary>
    /// 区块加载完成.
    /// </summary>
    public event EventHandler SectionInitialized;
}
