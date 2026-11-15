using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 上新日历页面视图模型.
/// </summary>
public sealed partial class CalendarPageViewModel : LayoutPageViewModelBase
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<CalendarPageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Dictionary<DateOnly, List<VideoItemViewModel>> _dateCache = [];
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarPageViewModel"/> class.
    /// </summary>
    public CalendarPageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<CalendarPageViewModel> logger,
        NavigationViewModel navigationViewModel,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _apiClient = apiClient;
        _logger = logger;
        _navigationViewModel = navigationViewModel;
        _dispatcherQueue = dispatcherQueue;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(CalendarPage);

    [RelayCommand]
    private async Task InitializeAsync() => await RequestDailyReleasesAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _dateCache.Clear();
        Timelines.Clear();
        SelectedTimeline = null;
        _isInitialized = false;
        IsError = false;
        UpdateVisibilityStates();
        await RequestDailyReleasesAsync();
    }

    [RelayCommand]
    private void SelectTimeline(TimelineItemViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        SelectedTimeline = vm;

        if (vm.Items is null)
        {
            if (_dateCache.TryGetValue(vm.TargetDate, out List<VideoItemViewModel>? cached))
            {
                vm.Items = cached;
                vm.Count = cached.Count;
                vm.IsEmpty = cached.Count == 0;
                UpdateVisibilityStates();
            }
            else
            {
                _ = LoadDateReleasesAsync(vm);
            }
        }
        else
        {
            UpdateVisibilityStates();
        }
    }

    [RelayCommand]
    private void ShowMyFavoriteCalendar()
    {
        _settings.LastSelectedFavoriteSection = "drama";
        _navigationViewModel.NavigateToOver(typeof(FavoritesPage));
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

            DailyReleasePage page = await _apiClient.GetDailyReleasesAsync(
                targetDate: null,
                offset: 0,
                sessionId: null,
                cancellationToken: ct
            );

            List<VideoItemViewModel> initialItems =
            [
                .. page.Items.Select(item => new VideoItemViewModel(item)),
            ];
            _dateCache[page.Date] = initialItems;

            IReadOnlyList<DateOnly> dates = page.AvailableDates is { Count: > 0 }
                ? page.AvailableDates
                : [.. Enumerable.Range(-7, 15).Select(offset => page.Date.AddDays(offset))];

            List<TimelineItemViewModel> viewModels = [];
            foreach (DateOnly date in dates)
            {
                bool isToday = date == page.Date;
                List<VideoItemViewModel>? items = isToday ? initialItems : null;
                viewModels.Add(new TimelineItemViewModel(date, isToday, items));
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

            DailyReleasePage page = await _apiClient.GetDailyReleasesAsync(
                targetDate: vm.TargetDate,
                offset: 0,
                sessionId: null,
                cancellationToken: ct
            );

            List<VideoItemViewModel> items =
            [
                .. page.Items.Select(item => new VideoItemViewModel(item)),
            ];
            _dateCache[vm.TargetDate] = items;

            _dispatcherQueue.TryEnqueue(() =>
            {
                vm.Items = items;
                vm.Count = items.Count;
                vm.IsEmpty = items.Count == 0;
                UpdateVisibilityStates();
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

    private void UpdateVisibilityStates()
    {
        ShowSideEmptyHolder =
            !IsTimelineLoading && (IsError || (_isInitialized && Timelines.Count == 0));
        ShowTimelineEmptyHolder = !IsTimelineLoading && (SelectedTimeline is { IsEmpty: true });
        IsEmpty = ShowSideEmptyHolder;
    }
}
