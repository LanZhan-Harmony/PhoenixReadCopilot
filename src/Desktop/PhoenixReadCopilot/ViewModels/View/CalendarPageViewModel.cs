using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 上新日历页面视图模型.
/// </summary>
internal sealed partial class CalendarPageViewModel : LayoutPageViewModelBase
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<CalendarPageViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IReservationService _reservationService;

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
    /// 时间线初始化完成.
    /// </summary>
    public event EventHandler? TimelineInitialized;

    /// <summary>
    /// 时间线短剧条目已更新（加载首屏或加载更多完成）.
    /// </summary>
    public event EventHandler? TimelineItemsUpdated;

    public CalendarPageViewModel(
        ICalendarService calendarService,
        ILogger<CalendarPageViewModel> logger,
        INavigationService navigationService,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService,
        IReservationService reservationService
    )
        : base(settingsService)
    {
        _calendarService = calendarService;
        _logger = logger;
        _navigationService = navigationService;
        _dispatcherQueue = dispatcherQueue;
        _reservationService = reservationService;

        _reservationService.ReservationChanged += OnReservationChanged;
    }

    private void OnReservationChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            foreach (TimelineItemViewModel timeline in Timelines)
            {
                foreach (DailyReleaseItemViewModel item in timeline.Items)
                {
                    if (item.Drama is not null && !string.IsNullOrEmpty(item.Drama.Id))
                    {
                        item.IsReserved = _reservationService.IsReserved(item.Drama.Id);
                    }
                }
            }
        });
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(CalendarPage);

    public async Task InitializeAsync() => await RequestDailyReleasesAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Timelines.Clear();
        SelectedTimeline = null;
        _isInitialized = false;
        IsError = false;
        UpdateVisibilityStates();
        await _reservationService.RefreshAsync();
        await RequestDailyReleasesAsync();
    }

    public void SelectTimeline(TimelineItemViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        SelectedTimeline = vm;

        if (!vm.IsLoaded)
        {
            _ = LoadDateReleasesAsync(vm);
        }
        else
        {
            UpdateVisibilityStates();
        }
    }

    [RelayCommand]
    private void ShowMyFavoriteCalendar()
    {
        _navigationService.NavigateToOver(typeof(UserSpacePage), "Reservation");
    }

    private async Task RequestDailyReleasesAsync()
    {
        if (IsTimelineLoading || Timelines.Count > 0)
        {
            return;
        }

        IsTimelineLoading = true;
        IsError = false;
        UpdateVisibilityStates();

        try
        {
            _timelineCts?.Cancel();
            _timelineCts?.Dispose();
            _timelineCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            CancellationToken ct = _timelineCts.Token;

            // 预热预约缓存
            try
            {
                await _reservationService.GetReservationsAsync(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"预热预约列表失败.");
            }

            DailyReleasePage page = await _calendarService.GetDailyReleasesAsync(
                targetDate: null,
                offset: 0,
                sessionId: null,
                cancellationToken: ct
            );

            List<DailyReleaseItemViewModel> initialItems =
            [
                .. page.Items.Select(item => new DailyReleaseItemViewModel(item)),
            ];

            IReadOnlyList<DateOnly> dates = page.AvailableDates is { Count: > 0 }
                ? page.AvailableDates
                : [.. Enumerable.Range(-7, 15).Select(offset => page.Date.AddDays(offset))];

            List<TimelineItemViewModel> viewModels = [];
            foreach (DateOnly date in dates)
            {
                bool isToday = date == page.Date;
                TimelineItemViewModel vm = new(date, isToday, isToday ? initialItems : null);
                if (isToday)
                {
                    vm.NextOffset = page.NextOffset;
                    vm.HasMore = page.HasMore;
                    vm.SessionId = page.SessionId;
                    vm.IsLoaded = true;
                }
                viewModels.Add(vm);
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                Timelines.Clear();
                foreach (TimelineItemViewModel vm in viewModels)
                {
                    Timelines.Add(vm);
                }

                TimelineItemViewModel? todayItem = Timelines.FirstOrDefault(p => p.IsToday)
                    ?? Timelines.FirstOrDefault();
                if (todayItem is not null)
                {
                    SelectTimeline(todayItem);
                }

                _isInitialized = true;
                UpdateVisibilityStates();
                TimelineInitialized?.Invoke(this, EventArgs.Empty);
                TimelineItemsUpdated?.Invoke(this, EventArgs.Empty);
            });
        }
        catch (OperationCanceledException)
        {
            // 取消请求，正常忽略
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取红果短剧上新日历失败.");
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsError = true;
                _isInitialized = true;
            });
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsTimelineLoading = false;
                UpdateVisibilityStates();
            });
        }
    }

    private async Task LoadDateReleasesAsync(TimelineItemViewModel vm)
    {
        try
        {
            IsTimelineLoading = true;
            UpdateVisibilityStates();
            _dateReleasesCts?.Cancel();
            _dateReleasesCts?.Dispose();
            _dateReleasesCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            CancellationToken ct = _dateReleasesCts.Token;

            DailyReleasePage page = await _calendarService.GetDailyReleasesAsync(
                targetDate: vm.TargetDate,
                offset: 0,
                sessionId: null,
                cancellationToken: ct
            );

            _dispatcherQueue.TryEnqueue(() =>
            {
                vm.Items.Clear();
                foreach (DailyReleaseItem item in page.Items)
                {
                    vm.Items.Add(new DailyReleaseItemViewModel(item));
                }

                vm.NextOffset = page.NextOffset;
                vm.HasMore = page.HasMore;
                vm.SessionId = page.SessionId;
                vm.IsLoaded = true;
                vm.Count = vm.Items.Count;
                vm.IsEmpty = vm.Items.Count == 0;
                UpdateVisibilityStates();
                TimelineItemsUpdated?.Invoke(this, EventArgs.Empty);
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取指定日期 ({vm.TargetDate}) 上新短剧失败.");
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsTimelineLoading = false;
                UpdateVisibilityStates();
            });
        }
    }

    /// <summary>
    /// 为当前选中的日期时间线请求更多短剧（触底加载）.
    /// </summary>
    public async Task RequestMoreDateReleasesAsync()
    {
        TimelineItemViewModel? current = SelectedTimeline;
        if (
            current is null
            || !current.IsLoaded
            || !current.HasMore
            || current.IsLoadingMore
            || IsTimelineLoading
        )
        {
            return;
        }

        current.IsLoadingMore = true;
        try
        {
            DailyReleasePage page = await _calendarService.GetDailyReleasesAsync(
                targetDate: current.TargetDate,
                offset: current.NextOffset,
                sessionId: current.SessionId
            );

            current.NextOffset = page.NextOffset;
            current.HasMore = page.HasMore;
            current.SessionId = page.SessionId;

            _dispatcherQueue.TryEnqueue(() =>
            {
                int added = 0;
                foreach (DailyReleaseItem item in page.Items)
                {
                    if (current.Items.Any(v => v.Drama?.Id == item.Drama.Id))
                    {
                        continue;
                    }

                    current.Items.Add(new DailyReleaseItemViewModel(item));
                    added++;
                }

                current.Count = current.Items.Count;
                current.IsEmpty = current.Items.Count == 0;
                UpdateVisibilityStates();

                if (added > 0)
                {
                    TimelineItemsUpdated?.Invoke(this, EventArgs.Empty);
                }
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取更多日期 ({current.TargetDate}) 上新短剧失败.");
        }
        finally
        {
            current.IsLoadingMore = false;
        }
    }

    private void UpdateVisibilityStates()
    {
        ShowSideEmptyHolder =
            !IsTimelineLoading && (IsError || (_isInitialized && Timelines.Count == 0));
        ShowTimelineEmptyHolder = !IsTimelineLoading && (SelectedTimeline is { IsEmpty: true });
        IsEmpty = ShowSideEmptyHolder;
    }
}
