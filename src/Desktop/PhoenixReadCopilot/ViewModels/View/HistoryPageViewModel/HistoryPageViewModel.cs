using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 历史记录页面视图模型.
/// </summary>
public sealed partial class HistoryPageViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryPageViewModel"/> class.
    /// </summary>
    public HistoryPageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<HistoryPageViewModel> logger,
        AppViewModel appViewModel,
        DispatcherQueue dispatcherQueue,
        ILoggerFactory loggerFactory
    )
    {
        _apiClient = apiClient;
        _logger = logger;
        _appViewModel = appViewModel;
        _dispatcherQueue = dispatcherQueue;
        _loggerFactory = loggerFactory;
    }

    [RelayCommand]
    private void Initialize()
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
                _apiClient,
                _loggerFactory.CreateLogger<VideoHistorySectionDetailViewModel>()
            ),
        ];

        Sections = sections;
        SelectSection(Sections.First());
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Search(string text)
    {
        ExitSearch();
        SearchKeyword = text;
        IsSearchMode = true;
        LoadMoreSearchResultCommand.Execute(null);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsSearchMode)
        {
            string keyword = SearchKeyword;
            SearchCommand.Execute(keyword);
            return;
        }

        if (SelectedSection is not null)
        {
            await SelectedSection.RefreshCommand.ExecuteAsync(this);
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
            foreach (var item in videoSection.Items.ToList())
            {
                if (item.HistoryEntry is not null)
                {
                    try
                    {
                        await _apiClient.DeleteHistoryAsync(item.HistoryEntry.SeriesId, item.HistoryEntry.VideoId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除历史记录出错: {SeriesId}", item.HistoryEntry.SeriesId);
                    }
                }
            }
        }

        await SelectedSection.RefreshCommand.ExecuteAsync(this);
    }

    [RelayCommand]
    private void SelectSection(IHistorySectionDetailViewModel section)
    {
        if (section is null || section == SelectedSection)
        {
            return;
        }

        SelectedSection = section;
        section.TryFirstLoadCommand.Execute(this);
    }

    [RelayCommand]
    private async Task LoadMoreSearchResultAsync()
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
            var ct = _searchCancellationTokenSource.Token;

            ReadHistoryPage historyPage = await _apiClient.GetHistoryAsync(
                count: 50,
                offset: _searchOffset,
                cancellationToken: ct
            );

            _preventLoadMoreSearch = !historyPage.HasMore || historyPage.Items.Count == 0;
            _searchOffset = historyPage.NextOffset;

            string kw = SearchKeyword.Trim();
            var filtered = historyPage.Items
                .Where(e => e.Title.Contains(kw, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (ReadHistoryEntry entry in filtered)
            {
                SearchVideos.Add(
                    new VideoItemViewModel(entry, RemoveSearchVideo)
                );
            }

            SearchUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (TaskCanceledException)
        {
            // Do nothing.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索历史记录失败");
            _appViewModel.ShowTipCommand.Execute(
                (Resources.FailedToSearchUserVideos, InfoType.Error)
            );
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
