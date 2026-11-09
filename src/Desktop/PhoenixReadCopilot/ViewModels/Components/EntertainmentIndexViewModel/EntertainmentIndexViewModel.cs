using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Models;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 短剧多维筛选索引视图模型.
/// </summary>
public sealed partial class EntertainmentIndexViewModel : ViewModelBase, IPgcSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntertainmentIndexViewModel"/> class.
    /// </summary>
    public EntertainmentIndexViewModel(
        PgcSectionType type,
        PhoenixReadApiClient apiClient,
        DispatcherQueue dispatcherQueue,
        ILogger<EntertainmentIndexViewModel> logger
    )
    {
        SectionType = type;
        _apiClient = apiClient;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Filters?.Count > 0)
        {
            return;
        }

        IsFilterLoading = true;
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            CancellationToken ct = _cancellationTokenSource.Token;

            IReadOnlyList<ShortDramaFilterRow> rows =
                await _apiClient.GetShortDramaFilterPanelAsync(ShortDramaGenre.ShortPlay, ct);
            List<IndexFilterViewModel> filterList = [];
            foreach (ShortDramaFilterRow row in rows)
            {
                string dimName = row.Name.StartsWith("全部") ? row.Name.Substring(2) : row.Name;
                List<Condition> conditions =
                [
                    new(row.Name, string.Empty),
                    .. row.Options.Select(opt => new Condition(opt.Name, opt.Value)),
                ];

                Filter filter = new(row.Key, dimName, conditions);
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
            _logger.LogError(ex, "尝试请求红果短剧筛选维度面板时失败.");
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsFilterLoading = false);
        }
    }

    [RelayCommand]
    private Task Refresh()
    {
        _offset = 0;
        _hasMore = true;
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            async () =>
            {
                Items.Clear();
                Filters = null;
                await InitializeAsync();
            }
        );

        return Task.CompletedTask;
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            CancellationToken ct = _cancellationTokenSource.Token;

            Dictionary<string, IReadOnlyList<string>> selections = [];
            if (Filters != null)
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
            _logger.LogError(ex, "尝试加载红果短剧筛选结果时失败.");
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
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () =>
            {
                Items.Clear();
                _ = RequestIndexAsync();
            }
        );
    }
}
