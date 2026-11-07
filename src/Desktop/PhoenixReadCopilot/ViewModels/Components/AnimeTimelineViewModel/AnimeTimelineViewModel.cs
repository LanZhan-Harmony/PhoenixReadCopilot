using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 短剧上新日历视图模型.
/// </summary>
public sealed partial class AnimeTimelineViewModel : ViewModelBase, IPgcSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnimeTimelineViewModel"/> class.
    /// </summary>
    public AnimeTimelineViewModel(
        PhoenixReadApiClient apiClient,
        DispatcherQueue dispatcherQueue,
        ILogger<AnimeTimelineViewModel> logger
    )
    {
        _apiClient = apiClient;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
    }

    private bool _isInitialized;

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Timelines.Count > 0)
        {
            return;
        }

        IsTimelineLoading = true;
        IsError = false;
        UpdateVisibilityStates();

        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var ct = _cancellationTokenSource.Token;

            var page = await _apiClient.GetDailyReleasesAsync(
                targetDate: null,
                offset: 0,
                sessionId: null,
                cancellationToken: ct
            );

            var initialItems = page.Items.Select(item => new SeasonItemViewModel(item)).ToList();
            _dateCache[page.Date] = initialItems;

            var dates = page.AvailableDates is { Count: > 0 }
                ? page.AvailableDates
                : Enumerable.Range(-7, 15).Select(offset => page.Date.AddDays(offset)).ToList();

            var viewModels = new List<TimelineItemViewModel>();
            foreach (var date in dates)
            {
                var isToday = date == page.Date;
                var items = isToday ? initialItems : null;
                viewModels.Add(new TimelineItemViewModel(date, isToday, items));
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                Timelines.Clear();
                foreach (var vm in viewModels)
                {
                    Timelines.Add(vm);
                }

                var todayItem = Timelines.FirstOrDefault(p => p.IsToday) ?? Timelines.FirstOrDefault();
                if (todayItem != null)
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
            _logger.LogError(ex, "获取红果短剧上新日历失败.");
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

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _dateCache.Clear();
        Timelines.Clear();
        SelectedTimeline = null;
        _isInitialized = false;
        IsError = false;
        UpdateVisibilityStates();
        await InitializeAsync();
    }

    [RelayCommand]
    private void SelectTimeline(TimelineItemViewModel vm)
    {
        if (vm is null)
        {
            return;
        }

        SelectedTimeline = vm;

        if (vm.Items == null)
        {
            if (_dateCache.TryGetValue(vm.TargetDate, out var cached))
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

    private async Task LoadDateReleasesAsync(TimelineItemViewModel vm)
    {
        try
        {
            IsTimelineLoading = true;
            UpdateVisibilityStates();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var ct = _cancellationTokenSource.Token;

            var page = await _apiClient.GetDailyReleasesAsync(
                targetDate: vm.TargetDate,
                offset: 0,
                sessionId: null,
                cancellationToken: ct
            );

            var items = page.Items.Select(item => new SeasonItemViewModel(item)).ToList();
            _dateCache[vm.TargetDate] = items;

            _dispatcherQueue.TryEnqueue(() =>
            {
                vm.Items = items;
                vm.Count = items.Count;
                vm.IsEmpty = items.Count == 0;
                UpdateVisibilityStates();
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取指定日期 ({Date}) 上新短剧失败.", vm.TargetDate);
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

    private void CheckEmpty() => UpdateVisibilityStates();

    private void UpdateVisibilityStates()
    {
        ShowSideEmptyHolder = !IsTimelineLoading && (IsError || (_isInitialized && Timelines.Count == 0));
        ShowTimelineEmptyHolder = !IsTimelineLoading && (SelectedTimeline is { IsEmpty: true });
        IsEmpty = ShowSideEmptyHolder;
    }
}
