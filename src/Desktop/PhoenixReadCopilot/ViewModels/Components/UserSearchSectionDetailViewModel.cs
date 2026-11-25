using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Search;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 用户/演职员搜索分区详情视图模型.
/// </summary>
internal sealed partial class UserSearchSectionDetailViewModel(
    ISearchService searchService,
    DispatcherQueue dispatcherQueue,
    ILogger<UserSearchSectionDetailViewModel> logger
) : ViewModelBase, ISearchSectionDetailViewModel
{
    private readonly ISearchService _searchService = searchService;
    private readonly ILogger<UserSearchSectionDetailViewModel> _logger = logger;
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;

    private bool _canRequest;
    private int _offset;
    private string _keyword = string.Empty;
    private SearchPartition? _partition;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <inheritdoc/>
    public SearchSectionType SectionType => SearchSectionType.User;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<UserItemViewModel> Items { get; } = [];

    /// <inheritdoc/>
    public void Initialize(string keyword, SearchPartition partition)
    {
        Clear();
        _partition = partition;
        IsEmpty = false;
        _canRequest = !IsEmpty;
        _keyword = keyword;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _partition = null;
        _keyword = string.Empty;
        _offset = 0;
        IsEmpty = false;
        _canRequest = false;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Items.Clear);
    }

    public Task TryFirstLoadAsync() => Items.Count > 0 ? Task.CompletedTask : LoadItemsAsync();

    public async Task LoadItemsAsync()
    {
        if (!_canRequest || IsEmpty || IsLoading || string.IsNullOrWhiteSpace(_keyword))
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
                    Offset = _offset,
                    Count = 20,
                    Tab = SearchTab.User,
                }
            );

            _offset = result.NextOffset;
            _canRequest = result.HasMore;

            foreach (User item in result.Users)
            {
                if (Items.Any(p => p.Id == item.Id))
                {
                    continue;
                }

                Items.Add(new UserItemViewModel(item));
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"加载用户搜索结果时出错.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        string keyword = _keyword;
        SearchPartition? partition = _partition;
        if (partition is not null)
        {
            Initialize(keyword, partition);
        }
        await LoadItemsAsync();
    }
}
