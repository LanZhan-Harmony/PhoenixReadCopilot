using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频/短剧历史记录视图模型.
/// </summary>
public sealed partial class VideoHistorySectionDetailViewModel
    : ViewModelBase,
        IHistorySectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoHistorySectionDetailViewModel"/> class.
    /// </summary>
    public VideoHistorySectionDetailViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<VideoHistorySectionDetailViewModel> logger
    )
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [RelayCommand]
    private async Task TryFirstLoadAsync()
    {
        if (Items.Count > 0 || _isPreventLoadMore)
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsEmpty = false;
        _isPreventLoadMore = false;
        Items.Clear();
        _offset = 0;
        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsEmpty || _isPreventLoadMore || IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ReadHistoryPage page = await _apiClient.GetHistoryAsync(
                count: 20,
                offset: _offset
            );
            _offset = page.NextOffset;
            _isPreventLoadMore = !page.HasMore || page.Items.Count == 0;
            if (page.Items.Count > 0)
            {
                foreach (ReadHistoryEntry entry in page.Items)
                {
                    if (Items.Any(p => p.HistoryEntry?.SeriesId == entry.SeriesId && p.HistoryEntry?.VideoId == entry.VideoId))
                    {
                        continue;
                    }

                    Items.Add(
                        new VideoItemViewModel(
                            entry,
                            removeAction: RemoveHistory
                        )
                    );
                }

                ListUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _isPreventLoadMore = true;
            _logger.LogError(ex, "加载短剧历史记录时出错.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
            IsLoading = false;
        }
    }

    private void RemoveHistory(VideoItemViewModel vm)
    {
        Items.Remove(vm);
        IsEmpty = Items.Count == 0;
    }
}
