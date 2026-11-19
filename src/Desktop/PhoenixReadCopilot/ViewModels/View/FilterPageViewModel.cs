using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Filter;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 多维筛选页面视图模型.
/// </summary>
public sealed partial class FilterPageViewModel : LayoutPageViewModelBase
{
    private readonly IFilterService _filterService;
    private readonly ILogger<FilterPageViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly DispatcherQueue _dispatcherQueue;
    private int _offset;
    private bool _hasMore;
    private string? _sessionId;
    private CancellationTokenSource? _filterCts;
    private CancellationTokenSource? _indexCts;

    [ObservableProperty]
    public partial List<IndexFilterViewModel>? Filters { get; set; }

    [ObservableProperty]
    public partial bool IsFilterLoading { get; set; }

    [ObservableProperty]
    public partial bool IsIndexLoading { get; set; }

    /// <summary>
    /// 条目已更新.
    /// </summary>
    public event EventHandler? ItemsUpdated;

    /// <summary>
    /// 短剧剧集列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Items { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPageViewModel"/> class.
    /// </summary>
    public FilterPageViewModel(
        IFilterService filterService,
        ILogger<FilterPageViewModel> logger,
        INavigationService navigationService,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _filterService = filterService;
        _logger = logger;
        _navigationService = navigationService;
        _dispatcherQueue = dispatcherQueue;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(FilterPage);

    public async Task InitializeAsync() => await RequestFilterPanelAsync();

    [RelayCommand]
    private Task Refresh()
    {
        _offset = 0;
        _hasMore = true;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
        {
            Items.Clear();
            Filters = null;
            await InitializeAsync();
        });

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void ShowMyFavoriteFilter()
    {
        _settings.LastSelectedFavoriteSection = "drama";
        _navigationService.NavigateToOver(typeof(FavoritePage));
    }

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
                await _filterService.GetFilterPanelAsync(ShortDramaGenre.ShortPlay, ct);
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

    public async Task RequestIndexAsync()
    {
        if (IsIndexLoading)
        {
            return;
        }

        if (!_hasMore && _offset > 0)
        {
            return;
        }

        IsIndexLoading = true;
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

            ShortDramaFilterPage page = await _filterService.FilterShortDramasAsync(query, ct);
            _offset = page.NextOffset;
            _hasMore = page.HasMore;
            _sessionId = page.SessionId;

            _dispatcherQueue.TryEnqueue(() =>
            {
                foreach (Drama drama in page.Items)
                {
                    Items.Add(new VideoItemViewModel(drama, VideoCardStyle.Filter));
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
            _dispatcherQueue.TryEnqueue(() => IsIndexLoading = false);
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
