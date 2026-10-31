using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Forms;
using PhoenixReadCopilot.UI.Models;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages.Overlay;
using PhoenixReadCopilot.UI.Toolkits;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 视频收藏分区详情视图模型.
/// </summary>
public sealed partial class VideoFavoriteSectionDetailViewModel
    : ViewModelBase<VideoFavoriteFolder>,
        IFavoriteSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoFavoriteSectionDetailViewModel"/> class.
    /// </summary>
    public VideoFavoriteSectionDetailViewModel(VideoFavoriteFolder data, IFavoriteService service)
        : base(data)
    {
        _service = service;
        _logger = this.Get<ILogger<VideoFavoriteSectionDetailViewModel>>();
    }

    internal void InjectFirstPageData(VideoFavoriteFolderDetail detail)
    {
        _pageNumber = 1;
        TotalCount = detail.TotalCount;
        foreach (var item in detail.Videos)
        {
            Items.Add(
                new VideoItemViewModel(
                    item,
                    Models.Constants.VideoCardStyle.Favorite,
                    removeAction: RemoveVideo,
                    favFolder: Data
                )
            );
        }

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task TryFirstLoadAsync()
    {
        if (Items.Count > 0 || _preventLoadMore)
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsEmpty = false;
        Items.Clear();
        _preventLoadMore = false;
        _pageNumber = 0;
        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsEmpty || IsLoading || _preventLoadMore)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var (detail, nextPn) = await _service.GetVideoFavoriteFolderDetailAsync(
                Data,
                _pageNumber
            );
            _preventLoadMore = detail is null || detail.Videos is null || nextPn is null;
            _pageNumber = nextPn ?? 0;
            TotalCount = detail.TotalCount;
            if (!_preventLoadMore || detail.Videos?.Count > 0)
            {
                foreach (var item in detail.Videos)
                {
                    Items.Add(
                        new VideoItemViewModel(
                            item,
                            Models.Constants.VideoCardStyle.Favorite,
                            removeAction: RemoveVideo,
                            favFolder: Data
                        )
                    );
                }

                ListUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _preventLoadMore = false;
            _logger.LogError(ex, $"获取视频收藏列表时出错.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void PlayCurrentList()
    {
        var preferDisplayMode = SettingsToolkit.ReadLocalSetting(
            SettingNames.DefaultPlayerDisplayMode,
            PlayerDisplayMode.Default
        );

        if (Items.Count == 1)
        {
            var video = Items.First();
            if (preferDisplayMode == PlayerDisplayMode.NewWindow)
            {
                new PlayerWindow().OpenVideo(new VideoSnapshot(video.Data));
                return;
            }

            this.Get<NavigationViewModel>()
                .NavigateToOver(typeof(VideoPlayerPage), new VideoSnapshot(video.Data));
        }
        else
        {
            var data = (Items.Select(p => p.Data).ToList(), new VideoSnapshot(Items.First().Data));
            if (preferDisplayMode == PlayerDisplayMode.NewWindow)
            {
                new PlayerWindow().OpenVideo(data);
                return;
            }

            this.Get<NavigationViewModel>().NavigateToOver(typeof(VideoPlayerPage), data);
        }
    }

    private void RemoveVideo(VideoItemViewModel vm)
    {
        Items.Remove(vm);
        IsEmpty = Items.Count == 0;
        ListUpdated?.Invoke(this, EventArgs.Empty);
    }
}
