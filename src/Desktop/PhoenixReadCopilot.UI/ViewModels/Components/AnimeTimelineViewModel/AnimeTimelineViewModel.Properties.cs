using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 动漫时间线视图模型.
/// </summary>
public sealed partial class AnimeTimelineViewModel
{
    private readonly IEntertainmentDiscoveryService _service;
    private readonly ILogger<AnimeTimelineViewModel> _logger;

    [ObservableProperty]
    public partial bool IsTimelineLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial TimelineItemViewModel SelectedTimeline { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TimelineItemViewModel> Timelines { get; set; } = new();

    /// <summary>
    /// 时间线加载完成.
    /// </summary>
    public event EventHandler TimelineInitialized;

    /// <inheritdoc/>
    public PgcSectionType SectionType => PgcSectionType.Timeline;
}
