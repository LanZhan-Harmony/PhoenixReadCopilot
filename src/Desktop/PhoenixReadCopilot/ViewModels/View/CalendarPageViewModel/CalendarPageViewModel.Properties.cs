using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 上新日历页面视图模型.
/// </summary>
public sealed partial class CalendarPageViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<CalendarPageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Dictionary<DateOnly, List<SeasonItemViewModel>> _dateCache = [];
    private CancellationTokenSource? _timelineCts;
    private CancellationTokenSource? _dateReleasesCts;
    private bool _isInitialized;

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
    public partial ObservableCollection<TimelineItemViewModel> Timelines { get; set; } = [];

    /// <summary>
    /// 时间线加载完成.
    /// </summary>
    public event EventHandler? TimelineInitialized;
}
