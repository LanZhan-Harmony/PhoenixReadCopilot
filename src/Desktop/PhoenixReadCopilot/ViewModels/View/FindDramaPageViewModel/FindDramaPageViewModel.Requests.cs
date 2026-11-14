using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Filter;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

public sealed partial class FindDramaPageViewModel
{
    private async Task RequestFilterPanelAsync()
    {
        if (IsFilterLoading || Filters?.Count > 0)
        {
            return;
        }

        IsFilterLoading = true;
        try
        {
            _filterCts?.Cancel();
            _filterCts?.Dispose();
            _filterCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            CancellationToken ct = _filterCts.Token;

            IReadOnlyList<ShortDramaFilterGroup> rows =
                await _apiClient.GetShortDramaFilterPanelAsync(ShortDramaGenre.ShortPlay, ct);
            List<IndexFilterViewModel> filterList = [];
            foreach (ShortDramaFilterGroup group in rows)
            {
                string dimName = group.Name.StartsWith("全部") ? group.Name[2..] : group.Name;
                List<Condition> conditions =
                [
                    new(group.Name, string.Empty),
                    .. group.Items.Select(opt => new Condition(opt.Name, opt.Value)),
                ];

                Filter filter = new(group.Key, dimName, conditions);
                filterList.Add(new IndexFilterViewModel(filter, RequestForConditionChanged));
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                Filters = filterList;
            });

            await RequestIndexAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试请求红果短剧筛选维度面板时失败.");
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsFilterLoading = false);
        }
    }

    [RelayCommand]
    private async Task RequestIndexAsync()
    {
        if (IsSeasonLoading)
        {
            return;
        }

        if (!_hasMore && _offset > 0)
        {
            return;
        }

        IsSeasonLoading = true;
        try
        {
            _indexCts?.Cancel();
            _indexCts?.Dispose();
            _indexCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            CancellationToken ct = _indexCts.Token;

            Dictionary<string, IReadOnlyList<string>> selections = [];
            if (Filters is not null)
            {
                foreach (IndexFilterViewModel f in Filters)
                {
                    if (
                        f.CurrentCondition is not null
                        && !string.IsNullOrEmpty(f.CurrentCondition.Value)
                    )
                    {
                        selections[f.Key] = [f.CurrentCondition.Value];
                    }
                }
            }

            ShortDramaFilterQuery query = new()
            {
                Genre = ShortDramaGenre.ShortPlay,
                Selections = selections,
                Offset = _offset,
                SessionId = _sessionId,
            };

            ShortDramaFilterPage page = await _apiClient.FilterShortDramasAsync(query, ct);
            _offset = page.NextOffset;
            _hasMore = page.HasMore;
            _sessionId = page.SessionId;

            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (Drama drama in page.Items)
                {
                    Items.Add(new SeasonItemViewModel(drama, SeasonCardStyle.Index));
                }

                ItemsUpdated?.Invoke(this, EventArgs.Empty);
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载红果短剧筛选结果时失败.");
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsSeasonLoading = false);
        }
    }

    private void RequestForConditionChanged()
    {
        _offset = 0;
        _hasMore = true;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            Items.Clear();
            _ = RequestIndexAsync();
        });
    }
}
