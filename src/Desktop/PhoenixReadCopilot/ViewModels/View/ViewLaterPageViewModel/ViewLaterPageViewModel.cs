using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 稍后再看视图模型.
/// </summary>
public sealed partial class ViewLaterPageViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewLaterPageViewModel"/> class.
    /// </summary>
    public ViewLaterPageViewModel(
        IViewLaterService service,
        ILogger<ViewLaterPageViewModel> logger,
        NavigationViewModel navigationViewModel,
        ISettingsService settingsService
    )
    {
        _service = service;
        _logger = logger;
        _navigationViewModel = navigationViewModel;
        _settings = settingsService.Settings;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Videos.Count > 0 || IsEmpty)
        {
            return;
        }

        await LoadVideosAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsEmpty = false;
        _pageNumber = 0;
        TotalCount = 0;
        Videos.Clear();
        await InitializeAsync();
    }

    [RelayCommand]
    private async Task LoadVideosAsync()
    {
        if (IsLoading || IsEmpty || (Videos.Count >= TotalCount && Videos.Count > 0))
        {
            return;
        }

        IsLoading = true;
        try
        {
            var (videos, totalCount, nextPage) = await _service.GetViewLaterSetAsync(_pageNumber);
            if (Videos.Count == 0 && (videos is null || videos.Count == 0 || totalCount == 0))
            {
                IsEmpty = true;
            }
            else
            {
                _pageNumber = nextPage;
                TotalCount = totalCount;
                foreach (var item in videos)
                {
                    if (Videos.Any(p => p.Data.Identifier.Id == item.Identifier.Id))
                    {
                        continue;
                    }

                    Videos.Add(
                        new VideoItemViewModel(
                            item,
                            VideoCardStyle.ViewLater,
                            removeAction: RemoveVideo
                        )
                    );
                }

                ListUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试加载稍后再看时出现异常");
        }
        finally
        {
            IsLoading = false;
            IsEmpty = Videos.Count == 0;
        }
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        try
        {
            await _service.CleanAsync(ViewLaterCleanType.All);
            await Task.Delay(500);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除稍后再看时出错");
        }
    }

    [RelayCommand]
    private void PlayAll()
    {
        var preferDisplayMode = _settings.DefaultPlayerDisplayMode;
        if (Videos.Count == 1)
        {
            var video = Videos.First();
            if (preferDisplayMode == PlayerDisplayMode.NewWindow)
            {
                new PlayerWindow().OpenVideo(new VideoSnapshot(video.Data));
                return;
            }

            _navigationViewModel.NavigateToOver(
                typeof(VideoPlayerPage),
                new VideoSnapshot(video.Data)
            );
        }
        else
        {
            var data = (
                Videos.Select(p => p.Data).ToList(),
                new VideoSnapshot(Videos.First().Data)
            );
            if (preferDisplayMode == PlayerDisplayMode.NewWindow)
            {
                new PlayerWindow().OpenVideo(data);
                return;
            }

            _navigationViewModel.NavigateToOver(typeof(VideoPlayerPage), data);
        }
    }

    private void RemoveVideo(VideoItemViewModel item)
    {
        Videos.Remove(item);
        IsEmpty = Videos.Count == 0;
    }
}
