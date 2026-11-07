using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 短剧上新日历视图模型.
/// </summary>
public sealed partial class AnimeTimelineViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<AnimeTimelineViewModel> _logger;
    private readonly Dictionary<DateOnly, List<SeasonItemViewModel>> _dateCache = new();
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    public partial bool IsTimelineLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsError { get; set; }

    [ObservableProperty]
    public partial bool ShowSideEmptyHolder { get; set; }

    [ObservableProperty]
    public partial bool ShowTimelineEmptyHolder { get; set; }

    [ObservableProperty]
    public partial TimelineItemViewModel? SelectedTimeline { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<TimelineItemViewModel> Timelines { get; set; } = new();

    /// <summary>
    /// 时间线加载完成.
    /// </summary>
    public event EventHandler? TimelineInitialized;

    /// <inheritdoc/>
    public PgcSectionType SectionType => PgcSectionType.Timeline;
}
