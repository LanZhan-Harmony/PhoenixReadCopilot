using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Models.Search;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 文章搜索分区详情视图模型.
/// </summary>
public sealed partial class ArticleSearchSectionDetailViewModel
    : ViewModelBase,
        ISearchSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArticleSearchSectionDetailViewModel"/> class.
    /// </summary>
    public ArticleSearchSectionDetailViewModel(
        DispatcherQueue dispatcherQueue,
        ILogger<ArticleSearchSectionDetailViewModel> logger
    )
    {
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Initialize(string keyword, SearchPartition partition)
    {
        Clear();
        _partition = partition;
        IsEmpty = true;
        _keyword = keyword;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _partition = null;
        _keyword = string.Empty;
        IsEmpty = false;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Items.Clear);
    }

    [RelayCommand]
    private Task TryFirstLoadAsync() => Items.Count > 0 ? Task.CompletedTask : LoadItemsAsync();

    [RelayCommand]
    private Task LoadItemsAsync()
    {
        IsEmpty = true;
        IsLoading = false;
        return Task.CompletedTask;
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
