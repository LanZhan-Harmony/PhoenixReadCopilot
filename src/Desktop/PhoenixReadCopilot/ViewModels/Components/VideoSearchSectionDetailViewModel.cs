using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Search;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频/短剧搜索分区详情视图模型.
/// </summary>
public sealed partial class VideoSearchSectionDetailViewModel
    : ViewModelBase,
        ISearchSectionDetailViewModel
{
    private readonly ISearchService _searchService;
    private readonly ILogger<VideoSearchSectionDetailViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    private bool _canRequest;
    private int? _currentPage;
    private string _keyword;
    private bool _isPreventLoadMore;
    private SearchPartition? _partition;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial ComprehensiveSearchSortType Sort { get; set; }

    [ObservableProperty]
    public partial List<ComprehensiveSearchSortType> Sorts { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <inheritdoc/>
    public SearchSectionType SectionType => SearchSectionType.Drama;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Items { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoSearchSectionDetailViewModel"/> class.
    /// </summary>
    public VideoSearchSectionDetailViewModel(
        ISearchService searchService,
        DispatcherQueue dispatcherQueue,
        ILogger<VideoSearchSectionDetailViewModel> logger
    )
    {
        _searchService = searchService;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
        Sorts = [.. Enum.GetValues<ComprehensiveSearchSortType>()];
    }

    /// <inheritdoc/>
    public void Initialize(string keyword, SearchPartition partition)
    {
        Clear();
        _partition = partition;
        Sort = ComprehensiveSearchSortType.Default;
        _keyword = keyword;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _keyword = string.Empty;
        IsEmpty = false;
        _isPreventLoadMore = false;
        _canRequest = false;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Items.Clear);
    }

    internal void SetFirstPageData(IReadOnlyList<Drama>? dramas, int? initialOffset)
    {
        _canRequest = true;
        _currentPage = initialOffset;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
            () =>
            {
                Items.Clear();
                _isPreventLoadMore = dramas is null || dramas.Count == 0;

                if (!_isPreventLoadMore && dramas is not null)
                {
                    foreach (Drama item in dramas)
                    {
                        if (Items.Any(p => p.Drama?.Id == item.Id))
                        {
                            continue;
                        }

                        Items.Add(new VideoItemViewModel(item, VideoCardStyle.Search));
                    }

                    ListUpdated?.Invoke(this, EventArgs.Empty);
                }

                IsEmpty = Items.Count == 0;
            });
    }

    public async Task TryFirstLoadAsync() => await Task.CompletedTask;

    public async Task LoadItemsAsync()
    {
        if (!_canRequest || IsEmpty || _isPreventLoadMore || IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            SearchResult result = await _searchService.SearchAsync(
                new SearchQuery
                {
                    Keyword = _keyword,
                    Offset = _currentPage ?? 0,
                    Count = 18,
                }
            );

            _currentPage = result.NextOffset;
            _canRequest = result.HasMore;
            _isPreventLoadMore = !result.HasMore || result.Dramas.Count == 0;

            foreach (Drama item in result.Dramas)
            {
                if (Items.Any(p => p.Drama?.Id == item.Id))
                {
                    continue;
                }

                Items.Add(new VideoItemViewModel(item, VideoCardStyle.Search));
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _isPreventLoadMore = true;
            _logger.ZLogError(ex, $"加载短剧搜索结果时出错.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
            async () =>
            {
                string keyword = _keyword;
                _currentPage = 0;
                Clear();
                _keyword = keyword;
                _canRequest = true;
                await LoadItemsAsync();
            });

        await Task.CompletedTask;
    }
}
