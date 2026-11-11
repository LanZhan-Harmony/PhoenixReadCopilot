using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

public sealed partial class CalendarPageViewModel
{
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

            List<SeasonItemViewModel> initialItems =
            [
                .. page.Items.Select(item => new SeasonItemViewModel(item)),
            ];
            _dateCache[page.Date] = initialItems;

            IReadOnlyList<DateOnly> dates = page.AvailableDates is { Count: > 0 }
                ? page.AvailableDates
                : [.. Enumerable.Range(-7, 15).Select(offset => page.Date.AddDays(offset))];

            List<TimelineItemViewModel> viewModels = [];
            foreach (DateOnly date in dates)
            {
                bool isToday = date == page.Date;
                List<SeasonItemViewModel>? items = isToday ? initialItems : null;
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

            List<SeasonItemViewModel> items =
            [
                .. page.Items.Select(item => new SeasonItemViewModel(item)),
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

    private void UpdateVisibilityStates()
    {
        ShowSideEmptyHolder =
            !IsTimelineLoading && (IsError || (_isInitialized && Timelines.Count == 0));
        ShowTimelineEmptyHolder = !IsTimelineLoading && (SelectedTimeline is { IsEmpty: true });
        IsEmpty = ShowSideEmptyHolder;
    }
}
