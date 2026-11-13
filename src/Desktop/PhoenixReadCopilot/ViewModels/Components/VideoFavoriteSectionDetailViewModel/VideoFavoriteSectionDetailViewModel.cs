using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;
using ZLogger;
using PhoenixReadCopilot.Core.Contracts.Services;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频/短剧收藏分区详情视图模型.
/// </summary>
public sealed partial class VideoFavoriteSectionDetailViewModel
    : ViewModelBase<VideoFavoriteFolder>,
        IFavoriteSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoFavoriteSectionDetailViewModel"/> class.
    /// </summary>
    public VideoFavoriteSectionDetailViewModel(
        VideoFavoriteFolder data,
        PhoenixReadApiClient apiClient,
        NavigationViewModel navigationViewModel,
        ISettingsService settingsService,
        ILogger<VideoFavoriteSectionDetailViewModel> logger
    )
        : base(data)
    {
        _apiClient = apiClient;
        _navigationViewModel = navigationViewModel;
        _settings = settingsService.Settings;
        _logger = logger;
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
        _offset = 0;
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
            DramaPage page = await _apiClient.GetFavoritesAsync(count: 20, offset: _offset);
            _preventLoadMore = !page.HasMore || page.Items.Count == 0;
            _offset = page.NextOffset;
            TotalCount = (int)page.Total;

            if (page.Items.Count > 0)
            {
                foreach (Drama drama in page.Items)
                {
                    if (Items.Any(p => p.Drama?.Id == drama.Id))
                    {
                        continue;
                    }

                    Items.Add(
                        new VideoItemViewModel(
                            drama,
                            VideoCardStyle.Favorite,
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
            _preventLoadMore = true;
            _logger.ZLogError(ex, $"加载短剧收藏失败");
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
        if (Items.Count == 0)
        {
            return;
        }

        VideoItemViewModel first = Items.First();
        first.PlayCommand.Execute(null);
    }

    private void RemoveVideo(VideoItemViewModel item)
    {
        Items.Remove(item);
        TotalCount = Math.Max(0, TotalCount - 1);
        IsEmpty = Items.Count == 0;
    }
}
