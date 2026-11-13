using CommunityToolkit.Mvvm.Input;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using Windows.ApplicationModel.DataTransfer;
using WinUIKernel.Share.Base;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 短剧播放器页面视图模型.
/// </summary>
public sealed partial class VideoPlayerPageViewModel
{
    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        try
        {
            string targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
            bool state = !IsLiked;
            if (!string.IsNullOrEmpty(_seriesId) && !string.IsNullOrEmpty(targetVideoId))
            {
                PhoenixOperationResult res = await _apiClient.SetDramaEpisodeLikeAsync(
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
            _logger.ZLogError(ex, $"尝试点赞/取消点赞分集时失败.");
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        try
        {
            bool state = !IsFavorited;
            if (!string.IsNullOrEmpty(_seriesId))
            {
                string targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
                PhoenixOperationResult res = await _apiClient.SetFavoriteAsync(
                    _seriesId,
                    targetVideoId,
                    state
                );
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
            _logger.ZLogError(ex, $"尝试收藏/取消收藏剧集时失败.");
        }
    }

    [RelayCommand]
    private Task FavoriteAsync() => ToggleFavoriteAsync();

    [RelayCommand]
    private async Task ShareAsync()
    {
        try
        {
            string targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
            ShareLink shareInfo = await _apiClient.CreateShareLinkAsync(_seriesId, targetVideoId);
            string url = !string.IsNullOrEmpty(shareInfo.Url) ? shareInfo.Url : shareInfo.ShortUrl;

            DataPackage package = new();
            package.SetText(url);
            package.SetWebLink(new Uri(url));
            Clipboard.SetContent(package);
            _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"分享短剧链接失败.");
        }
    }

    [RelayCommand]
    private async Task AddToViewLaterAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(_seriesId))
            {
                string targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
                await _apiClient.SetFavoriteAsync(_seriesId, targetVideoId, true);
                IsFavorited = true;
                _appViewModel.ShowTipCommand.Execute(("已加入追剧列表", InfoType.Success));
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"加入追剧列表失败");
            _appViewModel.ShowTipCommand.Execute(("加入追剧列表失败", InfoType.Error));
        }
    }

    [RelayCommand]
    private async Task PlayNextVideoAsync()
    {
        if (IsPageLoading || Player.IsPlayerDataLoading)
        {
            return;
        }

        object? nextPart = FindNextVideo();
        if (nextPart is null)
        {
            _appViewModel.ShowTipCommand.Execute(("已经是最后一集了", InfoType.Information));
            Player.BackToDefaultModeCommand.Execute(null);
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
            Player.BackToDefaultModeCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task PlayPreviousVideoAsync()
    {
        if (IsPageLoading || Player.IsPlayerDataLoading)
        {
            return;
        }

        object? prevPart = FindPreviousVideo();
        if (prevPart is null)
        {
            _appViewModel.ShowTipCommand.Execute(("已经是第一集了", InfoType.Information));
            return;
        }

        _initialProgress = 0;

        if (prevPart is Episode episode)
        {
            ChangePart(episode);
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
            Player.TogglePlayPauseCommand.Execute(null);
        }

        new PlayerWindow().OpenDrama(_seriesId);
    }

    [RelayCommand]
    private void Pin()
    {
        PinItem pinItem = new(
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
        DataPackage dp = new();
        dp.SetText(Title);
        Clipboard.SetContent(dp);
        _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
    }

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
        Downloader.CurrentEpisode = episode;
        Player.CancelNotification();

        Danmaku?.ResetData(_seriesId, episode.VideoId);
        var episodeDuration = (int)episode.Duration.TotalSeconds;
        Danmaku?.LoadDanmakusCommand.Execute(episodeDuration);
        Subtitle?.ResetData(_seriesId, episode.VideoId);

        // 加载分集媒体
        InitializeDashMediaCommand.Execute(episode);
        Subtitle.InitializeCommand.Execute(null);

        // 切换分集评论
        _comments.Initialize(_seriesId, episode.VideoId, CommentSortOrder.Hot);
        if (SelectedSection == _comments)
        {
            _comments.TryFirstLoadCommand.Execute(null);
        }

        InitializeNextVideo();
        VideoPlayerPartSectionDetailViewModel? partSection =
            Sections?.FirstOrDefault(p => p is VideoPlayerPartSectionDetailViewModel)
            as VideoPlayerPartSectionDetailViewModel;
        partSection?.UpdateSelectedPartCommand.Execute(episode);
    }

    private void PlayerProgressChanged(int progress, int duration)
    {
        // 兜底静默尝试：若当前分集尚未成功加载弹幕且未在加载中，发起一次加载尝试
        if (
            Danmaku is not null
            && Danmaku is { HasLoaded: false, IsLoading: false }
            && !string.IsNullOrEmpty(_seriesId)
            && !string.IsNullOrEmpty(_currentVideoId)
        )
        {
            Danmaku.LoadDanmakusCommand.Execute(duration);
        }

        Danmaku?.UpdatePosition(progress);
        Subtitle?.UpdatePosition(progress);
    }

    private void PlayerStateChanged(PlayerState state)
    {
        if (_currentEpisode is null && string.IsNullOrEmpty(_seriesId))
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
                Player.RestartCommand.Execute(null);
                return;
            }

            object? next = FindNextVideo();
            if (next is null)
            {
                if (!_isFormatChanging)
                {
                    Player.BackToDefaultModeCommand.Execute(null);
                }
                return;
            }

            // 判断下一项类型与对应自动播放设置
            bool shouldAutoPlay = false;
            if (next is Episode)
            {
                shouldAutoPlay = _settings.AutoPlayNext;
            }
            else if (next is Drama)
            {
                shouldAutoPlay = _settings.AutoPlayNextRecommendVideo;
            }

            if (!shouldAutoPlay)
            {
                if (!_isFormatChanging)
                {
                    Player.BackToDefaultModeCommand.Execute(null);
                }
                return;
            }

            if (IsPageLoading || Player.IsPlayerDataLoading)
            {
                return;
            }

            // 直接播放下一集或下一个短剧，无需触发倒计时浮层弹窗
            PlayNextVideoCommand.Execute(null);
        });
    }
}
