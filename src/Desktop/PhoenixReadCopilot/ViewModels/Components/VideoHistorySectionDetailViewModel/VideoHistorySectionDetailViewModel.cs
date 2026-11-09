using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频历史记录视图模型.
/// </summary>
public sealed partial class VideoHistorySectionDetailViewModel
    : ViewModelBase,
        IHistorySectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoHistorySectionDetailViewModel"/> class.
    /// </summary>
    public VideoHistorySectionDetailViewModel(
        IViewHistoryService service,
        ILogger<VideoHistorySectionDetailViewModel> logger
    )
    {
        _service = service;
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
            ViewHistoryGroup? group = await _service.GetViewHistoryAsync(Type, _offset);
            _offset = group.Offset ?? 0;
            _isPreventLoadMore = group is null || _offset == 0;
            if (group.Videos is not null)
            {
                foreach (VideoInformation video in group.Videos)
                {
                    Items.Add(
                        new VideoItemViewModel(
                            video,
                            VideoCardStyle.History,
                            removeAction: RemoveHistory
                        )
                    );
                }
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _isPreventLoadMore = true;
            _logger.LogError(ex, "加载视频历史记录时出错.");
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
