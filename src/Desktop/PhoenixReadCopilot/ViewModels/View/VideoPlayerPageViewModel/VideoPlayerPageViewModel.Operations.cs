using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Appearance;
using Richasy.BiliKernel.Models.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 短剧播放器页面视图模型.
/// </summary>
public sealed partial class VideoPlayerPageViewModel
{
    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        try
        {
            var firstCredit = _dramaDetails?.Drama?.Credits?.FirstOrDefault();
            if (firstCredit?.User is { } user && !string.IsNullOrEmpty(user.Id))
            {
                var state = !IsFollow;
                var res = await _apiClient.SetFollowAsync(user.Id, state);
                if (res.Succeeded)
                {
                    IsFollow = state;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试关注/取消关注制作人员时失败.");
        }
    }

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        try
        {
            var targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
            var state = !IsLiked;
            if (!string.IsNullOrEmpty(_seriesId) && !string.IsNullOrEmpty(targetVideoId))
            {
                var res = await _apiClient.SetDramaEpisodeLikeAsync(
                    _seriesId,
                    targetVideoId,
                    state
                );
                if (res.Succeeded)
                {
                    IsLiked = state;
                    LikeCount += state ? 1 : -1;
                }
                else
                {
                    _appViewModel.ShowTipCommand.Execute(
                        (res.Message ?? "点赞操作失败", InfoType.Warning)
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试点赞/取消点赞分集时失败.");
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        try
        {
            var state = !IsFavorited;
            if (!string.IsNullOrEmpty(_seriesId))
            {
                var targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
                var res = await _apiClient.SetFavoriteAsync(_seriesId, targetVideoId, state);
                if (res.Succeeded)
                {
                    IsFavorited = state;
                    FavoriteCount += state ? 1 : -1;
                }
                else
                {
                    _appViewModel.ShowTipCommand.Execute(
                        (res.Message ?? "收藏操作失败", InfoType.Warning)
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试收藏/取消收藏剧集时失败.");
        }
    }

    [RelayCommand]
    private Task FavoriteAsync() => ToggleFavoriteAsync();

    [RelayCommand]
    private async Task ShareAsync()
    {
        try
        {
            var targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
            var shareInfo = await _apiClient.CreateShareLinkAsync(_seriesId, targetVideoId);
            var url = !string.IsNullOrEmpty(shareInfo.Url)
                ? shareInfo.Url
                : (!string.IsNullOrEmpty(shareInfo.ShortUrl) ? shareInfo.ShortUrl : GetWebLink());

            var package = new DataPackage();
            package.SetText(url);
            package.SetWebLink(new Uri(url));
            Clipboard.SetContent(package);
            _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分享短剧链接失败.");
        }
    }

    [RelayCommand]
    private void ShareVideoUrl() => ShareCommand.Execute(default);

    [RelayCommand]
    private void CopyVideoUrl()
    {
        var url = GetWebLink();
        var dp = new DataPackage();
        dp.SetText(url);
        dp.SetWebLink(new Uri(url));
        Clipboard.SetContent(dp);
        _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
    }

    [RelayCommand]
    private async Task AddToViewLaterAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_seriesId))
            {
                var targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
                await _apiClient.SetFavoriteAsync(_seriesId, targetVideoId, true);
                IsFavorited = true;
                _appViewModel.ShowTipCommand.Execute(("已加入追剧列表", InfoType.Success));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加入追剧列表失败");
            _appViewModel.ShowTipCommand.Execute(("加入追剧列表失败", InfoType.Error));
        }
    }

    [RelayCommand]
    private Task OpenInBroswerAsync() => Launcher.LaunchUriAsync(new Uri(GetWebLink())).AsTask();

    [RelayCommand]
    private async Task PlayNextVideoAsync()
    {
        if (IsPageLoading || Player.IsPlayerDataLoading)
        {
            return;
        }

        var nextPart = FindNextVideo();
        if (nextPart is null)
        {
            Player.BackToDefaultModeCommand.Execute(default);
            return;
        }

        _initialProgress = 0;

        if (nextPart is Episode episode)
        {
            ChangePart(episode);
        }
        else if (nextPart is Drama rec)
        {
            InitializePageCommand.Execute(rec.Id);
        }
        else
        {
            Player.BackToDefaultModeCommand.Execute(default);
        }
    }

    [RelayCommand]
    private void OpenUserSpace()
    {
        var firstCredit = _dramaDetails?.Drama?.Credits?.FirstOrDefault();
        if (firstCredit?.User is { } user && !string.IsNullOrEmpty(user.Id))
        {
            var avatarImage =
                !string.IsNullOrEmpty(user.AvatarUrl)
                && Uri.TryCreate(user.AvatarUrl, UriKind.Absolute, out var uri)
                    ? new BiliImage(uri)
                    : default;
            var profile = new Richasy.BiliKernel.Models.User.UserProfile(
                user.Id,
                user.Name,
                avatarImage
            );
            _navigationViewModel.NavigateToOver(typeof(UserSpacePage), profile);
        }
    }

    [RelayCommand]
    private void OpenInNewWindow()
    {
        if (Player.Position > 0)
        {
            ReportProgressCommand.Execute(Player.Position);
        }

        if (!Player.IsPaused)
        {
            Player.TogglePlayPauseCommand.Execute(default);
        }

        var coverImage = Cover != null ? new BiliImage(Cover) : default;
        var info = new VideoInformation(new MediaIdentifier(_seriesId, Title, coverImage), default);
        new PlayerWindow().OpenVideo(new VideoSnapshot(info, IsPrivatePlay));
    }

    [RelayCommand]
    private void Pin()
    {
        var pinItem = new PinItem(
            _seriesId,
            Title,
            Cover?.ToString() ?? string.Empty,
            PinContentType.Video
        );
        _pinnerViewModel.AddItemCommand.Execute(pinItem);
    }

    [RelayCommand]
    private void CopyTitle()
    {
        var dp = new DataPackage();
        dp.SetText(Title);
        Clipboard.SetContent(dp);
        _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
    }

    private string GetWebLink() =>
        !string.IsNullOrEmpty(_seriesId)
            ? $"https://novel.snssdk.com/page/drama?book_id={_seriesId}"
            : "https://novel.snssdk.com";

    private void ChangePart(Episode episode)
    {
        if (episode is null)
        {
            return;
        }

        if (_initialProgress == 0)
        {
            _initialProgress = -1;
        }

        _currentEpisode = episode;
        _currentVideoId = episode.VideoId;
        Player.CancelNotification();

        Danmaku?.ResetData(_seriesId, episode.VideoId);
        Subtitle?.ResetData(_seriesId, episode.VideoId);

        // 加载分集媒体
        InitializeDashMediaCommand.Execute(episode);
        Subtitle.InitializeCommand.Execute(default);

        // 切换分集评论
        _comments.Initialize(_seriesId, episode.VideoId, CommentSortType.Hot);

        InitializeNextVideo();
        var partSection =
            Sections?.FirstOrDefault(p => p is VideoPlayerPartSectionDetailViewModel)
            as VideoPlayerPartSectionDetailViewModel;
        partSection?.UpdateSelectedPartCommand.Execute(episode);
    }

    private void PlayerProgressChanged(int progress, int duration)
    {
        if (progress < duration && progress > 1 && (Danmaku?.IsEmpty() ?? false))
        {
            Danmaku?.ResetData(_seriesId, _currentEpisode?.VideoId ?? _currentVideoId);
            Danmaku?.LoadDanmakusCommand.Execute(duration);
        }

        Danmaku?.UpdatePosition(progress);
        Subtitle?.UpdatePosition(progress);
    }

    private void PlayerStateChanged(PlayerState state)
    {
        if (_currentEpisode == null && string.IsNullOrEmpty(_seriesId))
        {
            return;
        }

        if (state == PlayerState.Playing)
        {
            Danmaku?.Resume();
        }
        else
        {
            if (state == PlayerState.Paused)
            {
                // 记录播放进度.
                ReportProgressCommand.Execute(Player.Position);
            }

            Danmaku?.Pause();
        }
    }

    private void PlayerSpeedChanged(double speed)
    {
        Danmaku?.ExtraSpeed = speed;
    }

    private void PlayerMediaEnded()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // 清除弹幕.
            Danmaku.ClearDanmaku();
            Subtitle.ClearSubtitle();

            ReportProgressCommand.Execute(Player.Duration);

            // 单视频循环.
            if (CurrentLoop == VideoLoopType.Single)
            {
                Player.RestartCommand.Execute(default);
                return;
            }

            var autoNext = _settings.AutoPlayNext;
            if ((!autoNext || !HasNextVideo) && !_isFormatChanging)
            {
                Player.BackToDefaultModeCommand.Execute(default);
                return;
            }

            if (IsPageLoading || Player.IsPlayerDataLoading)
            {
                return;
            }

            var next = FindNextVideo();
            if (next is null)
            {
                return;
            }

            var withoutTip = _settings.PlayNextWithoutTip;
            if (withoutTip)
            {
                PlayNextVideoCommand.Execute(default);
            }
            else
            {
                string tip = default!;
                if (next is Episode episode)
                {
                    tip = string.Format(
                        Resources.NextVideoNotificationTemplate,
                        $"第 {episode.Number} 集"
                    );
                }
                else if (next is Drama rec)
                {
                    tip = string.Format(Resources.NextVideoNotificationTemplate, rec.Title);
                }

                var notification = new PlayerNotification(
                    () => PlayNextVideoCommand.Execute(default),
                    tip,
                    5
                );
                Player.ShowNotification(notification);
            }
        });
    }
}
