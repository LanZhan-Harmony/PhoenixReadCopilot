using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 动漫页面视图模型.
/// </summary>
public sealed partial class AnimePageViewModel
{
    private readonly IEntertainmentDiscoveryService _service;
    private readonly ILogger<AnimePageViewModel> _logger;

    [ObservableProperty] private List<IPgcSectionDetailViewModel>? _sections;

    [ObservableProperty] private IPgcSectionDetailViewModel? _selectedSection;

    /// <summary>
    /// 区块加载完成.
    /// </summary>
    public event EventHandler SectionInitialized;
}
