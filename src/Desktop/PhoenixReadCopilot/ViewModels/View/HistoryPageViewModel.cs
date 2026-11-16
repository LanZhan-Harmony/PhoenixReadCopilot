using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 历史记录页面视图模型.
/// </summary>
public sealed partial class HistoryPageViewModel : ViewModelBase
{
    private readonly IHistoryService _historyService;
    private readonly ILogger<HistoryPageViewModel> _logger;
    private readonly AppViewModel _appViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILoggerFactory _loggerFactory;
    private int _searchOffset;
    private bool _preventLoadMoreSearch;
    private CancellationTokenSource? _searchCancellationTokenSource;

    [ObservableProperty]
    public partial IHistorySectionDetailViewModel? SelectedSection { get; set; }

    [ObservableProperty]
    public partial List<IHistorySectionDetailViewModel>? Sections { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial bool IsSearchMode { get; set; }

    [ObservableProperty]
    public partial string SearchKeyword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSearchEmpty { get; set; }

    /// <summary>
    /// 初始化完成.
    /// </summary>
    public event EventHandler? SectionInitialized;

    /// <summary>
    /// 搜索更新.
    /// </summary>
    public event EventHandler? SearchUpdated;

    /// <summary>
    /// 搜索视频结果.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> SearchVideos { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryPageViewModel"/> class.
    /// </summary>
    public HistoryPageViewModel(
        IHistoryService historyService,
        ILogger<HistoryPageViewModel> logger,
        AppViewModel appViewModel,
        DispatcherQueue dispatcherQueue,
        ILoggerFactory loggerFactory
    )
    {
        _historyService = historyService;
        _logger = logger;
        _appViewModel = appViewModel;
        _dispatcherQueue = dispatcherQueue;
        _loggerFactory = loggerFactory;
    }

    [RelayCommand]
    public void Initialize()
    {
        if (IsSearchMode)
        {
            ExitSearch();
        }

        if (Sections is not null)
        {
            return;
        }

        List<IHistorySectionDetailViewModel> sections =
        [
            new VideoHistorySectionDetailViewModel(
                _historyService,
                _loggerFactory.CreateLogger<VideoHistorySectionDetailViewModel>()
            ),
        ];

        Sections = sections;
        SelectSection(Sections.First());
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 搜索历史记录.
    /// </summary>
    public void Search(string text)
    {
        ExitSearch();
        SearchKeyword = text;
        IsSearchMode = true;
        _ = LoadMoreSearchResultAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsSearchMode)
        {
            string keyword = SearchKeyword;
            Search(keyword);
            return;
        }

        if (SelectedSection is not null)
        {
            await SelectedSection.RefreshAsync();
        }
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        if (SelectedSection is null)
        {
            return;
        }

        if (SelectedSection is VideoHistorySectionDetailViewModel videoSection)
        {
            foreach (VideoItemViewModel? item in videoSection.Items.ToList())
            {
                if (item.HistoryItem is not null)
                {
                    try
                    {
                        await _historyService.DeleteHistoryAsync(
                            item.HistoryItem.DramaId,
                            item.HistoryItem.VideoId
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.ZLogWarning(ex, $"删除历史记录出错: {item.HistoryItem.DramaId}");
                    }
                }
            }
        }

        await SelectedSection.RefreshAsync();
    }

    public void SelectSection(IHistorySectionDetailViewModel section)
    {
        if (section is null || section == SelectedSection)
        {
            return;
        }

        SelectedSection = section;
        _ = section.TryFirstLoadAsync();
    }

    public async Task LoadMoreSearchResultAsync()
    {
        if (
            _preventLoadMoreSearch
            || IsSearching
            || !IsSearchMode
            || string.IsNullOrEmpty(SearchKeyword)
        )
        {
            return;
        }

        try
        {
            IsSearching = true;
            CancelSearch();
            _searchCancellationTokenSource = new CancellationTokenSource();
            CancellationToken ct = _searchCancellationTokenSource.Token;

            ReadHistoryPage historyPage = await _historyService.GetHistoryAsync(
                count: 50,
                offset: _searchOffset,
                cancellationToken: ct
            );

            _preventLoadMoreSearch = !historyPage.HasMore || historyPage.Items.Count == 0;
            _searchOffset = historyPage.NextOffset;

            string kw = SearchKeyword.Trim();
            List<ReadHistoryItem> filtered =
            [
                .. historyPage.Items.Where(e =>
                    e.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)
                ),
            ];

            foreach (ReadHistoryItem item in filtered)
            {
                SearchVideos.Add(new VideoItemViewModel(item, RemoveSearchVideo));
            }

            SearchUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (TaskCanceledException)
        {
            // Do nothing.
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"搜索历史记录失败");
            _appViewModel.ShowTip(Resources.FailedToSearchUserVideos, InfoType.Error);
        }
        finally
        {
            IsSearchEmpty = SearchVideos.Count == 0;
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void ExitSearch()
    {
        IsSearchMode = false;
        SearchKeyword = string.Empty;
        IsSearchEmpty = false;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, SearchVideos.Clear);
        _preventLoadMoreSearch = false;
        _searchOffset = 0;
        CancelSearch();
    }

    private void CancelSearch()
    {
        if (_searchCancellationTokenSource is not null)
        {
            _searchCancellationTokenSource.Cancel();
            _searchCancellationTokenSource.Dispose();
            _searchCancellationTokenSource = null;
        }
    }

    private void RemoveSearchVideo(VideoItemViewModel item)
    {
        SearchVideos.Remove(item);
        if (SearchVideos.Count == 0)
        {
            IsSearchEmpty = true;
        }

        SearchUpdated?.Invoke(this, EventArgs.Empty);
    }
}
