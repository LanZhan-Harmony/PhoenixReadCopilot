using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 短剧播放器页面视图模型.
/// </summary>
public sealed partial class VideoPlayerPageViewModel
{
    private void InitializeDramaView(DramaDetails details)
    {
        Drama drama = details.Drama;
        _seriesId = drama.Id;
        Title = drama.Title;
        Description = drama.Description;
        Cover =
            !string.IsNullOrEmpty(drama.CoverUrl)
            && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out Uri? coverUri)
                ? coverUri
                : null;
        PlayCount =
            drama.PlayCount > 0
                ? drama.PlayCount
                : (double.TryParse(drama.Views, out double vc) ? vc : 0);
        LikeCount = drama.DiggCount;
        FavoriteCount = drama.FollowCount;
        OnlineCountText = !string.IsNullOrEmpty(drama.Heat)
            ? drama.Heat
            : (!string.IsNullOrEmpty(drama.Score) ? $"{drama.Score} 分" : "热播中");

        PublishRelativeTime = !string.IsNullOrEmpty(drama.OnlineDate)
            ? drama.OnlineDate
            : "红果独播";

        Credits =
            drama
                .Credits?.Select(c => new DramaCreditItemViewModel(c, _apiClient, _appViewModel))
                .ToList()
            ?? [];
        Tags = drama.Tags?.ToList() ?? [];

        Player.Title = Title;
        CalcPlayerHeight();
    }

    private void InitializePlaybackMedias(IReadOnlyList<PlaybackMedia> mediaList)
    {
        Downloader.Clear();
        Downloader.InitializeDrama(
            _seriesId,
            Title,
            Cover?.ToString() ?? string.Empty,
            _episodes,
            _currentEpisode
        );

        // 按清晰度文案去重，每种清晰度只保留一条，优先保留带解密密钥与高码率的
        List<PlaybackMedia> distinctMedias =
        [
            .. mediaList
                .GroupBy(PlayerFormatItemViewModel.GetFormatKey)
                .Select(g =>
                    g.OrderByDescending(m => !string.IsNullOrEmpty(m.DecryptionKey))
                        .ThenByDescending(m => m.Bitrate)
                        .First()
                )
                .OrderByDescending(PlayerFormatItemViewModel.GetQualityScore),
        ];

        Formats = [.. distinctMedias.Select(m => new PlayerFormatItemViewModel(m))];

        int partIndex =
            _episodes is not null && _currentEpisode is not null
                ? _episodes.ToList().FindIndex(e => e.VideoId == _currentEpisode.VideoId)
                : 0;

        Downloader.InitializeMetas(
            GetWebLink(),
            [.. Formats.Select(p => p.Data)],
            partIndex >= 0 ? partIndex + 1 : 1
        );

        // 选择最佳画质或用户偏好画质
        PlayerFormatItemViewModel? selectedFormat = Formats.FirstOrDefault();
        int lastSelectedQuality = _settings.LastSelectedVideoQuality;
        if (lastSelectedQuality > 0)
        {
            PlayerFormatItemViewModel? matched = Formats.Find(p =>
                p.Data.Quality == lastSelectedQuality
            );
            if (matched is not null)
            {
                selectedFormat = matched;
            }
        }

        ChangeFormatCommand.Execute(selectedFormat);
    }

    private async Task InitializeSectionsAsync()
    {
        if (Sections?.Count > 0)
        {
            return;
        }

        List<IPlayerSectionDetailViewModel> sections =
        [
            new VideoPlayerInfoSectionDetailViewModel(this),
        ];

        // 1. 分集列表（网格序号模式）
        if (_episodes?.Count > 1)
        {
            sections.Add(
                new VideoPlayerPartSectionDetailViewModel(
                    _episodes,
                    _currentEpisode?.VideoId ?? string.Empty,
                    ChangePart,
                    _settingsService
                )
            );
        }

        // 2. 剧评 Tab（整部剧一份，显示评分与发表评论）
        sections.Add(_dramaReviews);

        // 3. 评论列表 Tab（分集评论）
        sections.Add(_comments);

        // 4. 专属关联好剧推荐（调用红果推荐接口）
        try
        {
            DramaRelatedRecommendations rec = await _apiClient.GetSeriesRecommendationsAsync(
                _seriesId
            );
            if (rec?.Items is { Count: > 0 } recItems)
            {
                sections.Add(new VideoPlayerRecommendSectionDetailViewModel(recItems));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取推荐好剧失败.");
        }

        Sections = sections;
        SelectSection(Sections.First());
        InitializeNextVideo();
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    private void ClearView()
    {
        IsPageLoadFailed = false;
        _episodes = null;
        _currentEpisode = null;
        _dramaDetails = null;
        _initialProgress = -1;
        Tags = null;
        IsFollow = false;
        IsMyVideo = false;
        PlayCount = 0;
        CommentCount = 0;
        LikeCount = 0;
        FavoriteCount = 0;
        IsLiked = false;
        IsFavorited = false;
        FavoriteFolders = null;
        SelectedFormat = null;
        Sections?.Clear();
    }

    private void CalcPlayerHeight()
    {
        if (PlayerWidth <= 0 || Player.IsFullScreen || Player.IsCompactOverlay)
        {
            return;
        }

        // 红果短剧默认为 9:16 竖屏短剧
        double ratioWidth = 9.0;
        double ratioHeight = 16.0;
        if (_currentEpisode?.IsVertical == false)
        {
            ratioWidth = 16.0;
            ratioHeight = 9.0;
        }

        PlayerHeight = PlayerWidth * ratioHeight / ratioWidth;
    }

    private Episode? FindInitialPart(string? initialVideoId = null)
    {
        if (_episodes?.Count == 1)
        {
            return _episodes.First();
        }

        Episode? part = null;
        if (!string.IsNullOrEmpty(initialVideoId) && _episodes is not null)
        {
            part = _episodes.FirstOrDefault(p => p.VideoId == initialVideoId);
        }

        return part ?? _episodes?.FirstOrDefault();
    }

    private void LoadInitialProgress(int? progress = null)
    {
        _initialProgress = 0;
        if (progress is not null)
        {
            _initialProgress = progress.Value;
        }
    }

    private object? FindNextVideo()
    {
        bool isListLoop = CurrentLoop == VideoLoopType.List;

        // 1. 先检查分集列表中是否有下一集.
        if (_episodes is not null && _currentEpisode is not null)
        {
            List<Episode> list = [.. _episodes];
            int index = list.FindIndex(e => e.VideoId == _currentEpisode.VideoId);
            if (index >= 0 && index < list.Count - 1)
            {
                return list[index + 1];
            }

            if (isListLoop && list.Count > 0)
            {
                return list[0];
            }
        }

        // 2. 检查推荐好剧中是否有下一个短剧.
        bool isAutoPlayRecommendVideo = _settings.AutoPlayNextRecommendVideo;
        if (
            isAutoPlayRecommendVideo
            && Sections?.OfType<VideoPlayerRecommendSectionDetailViewModel>().FirstOrDefault()
                is VideoPlayerRecommendSectionDetailViewModel recommendSection
        )
        {
            VideoItemViewModel? firstItem = recommendSection.Items.FirstOrDefault();
            return firstItem?.Drama ?? (object?)firstItem?.Data;
        }

        return null;
    }

    private object? FindPreviousVideo()
    {
        bool isListLoop = CurrentLoop == VideoLoopType.List;

        if (_episodes is not null && _currentEpisode is not null)
        {
            List<Episode> list = [.. _episodes];
            int index = list.FindIndex(e => e.VideoId == _currentEpisode.VideoId);
            if (index > 0)
            {
                return list[index - 1];
            }

            if (isListLoop && list.Count > 0)
            {
                return list[^1];
            }
        }

        return null;
    }

    /// <summary>
    /// 后台轻量预加载下一集媒体播放元数据，不启动播放器实例，实现秒切并保持低内存.
    /// </summary>
    private void PreloadNextEpisodeMedia()
    {
        object? next = FindNextVideo();
        if (
            next is not Episode nextEpisode
            || string.IsNullOrEmpty(_seriesId)
            || string.IsNullOrEmpty(nextEpisode.VideoId)
        )
        {
            return;
        }

        if (
            _preloadedEpisodeVideoId == nextEpisode.VideoId
            && _preloadedMediaList is { Count: > 0 }
        )
        {
            return;
        }

        _preloadCancellationTokenSource?.Cancel();
        _preloadCancellationTokenSource?.Dispose();
        _preloadCancellationTokenSource = new CancellationTokenSource();
        CancellationToken token = _preloadCancellationTokenSource.Token;

        _ = Task.Run(
            async () =>
            {
                try
                {
                    // 等待 1 秒避开当前集起播的网络高峰
                    await Task.Delay(1000, token);
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    IReadOnlyList<PlaybackMedia> mediaList = await _apiClient.ResolveMediaAsync(
                        _seriesId,
                        nextEpisode.VideoId,
                        token
                    );
                    if (mediaList is { Count: > 0 } && !token.IsCancellationRequested)
                    {
                        _preloadedEpisodeVideoId = nextEpisode.VideoId;
                        _preloadedMediaList = mediaList;

                        // 进一步在后台预载下一集 FFmpegMediaSource（提前解析 demuxer 与解密密钥，实现 0 秒瞬播）
                        int targetQuality = _settings.LastSelectedVideoQuality;
                        PlaybackMedia? matchedMedia = null;
                        if (targetQuality > 0)
                        {
                            matchedMedia = mediaList.FirstOrDefault(m =>
                                PlayerFormatItemViewModel.GetQualityScore(m) == targetQuality
                            );
                        }
                        matchedMedia ??= mediaList
                            .OrderByDescending(PlayerFormatItemViewModel.GetQualityScore)
                            .FirstOrDefault();

                        if (
                            matchedMedia is not null
                            && !string.IsNullOrEmpty(matchedMedia.Url)
                            && Player is FFmpegPlayerViewModel ffmpegVm
                        )
                        {
                            await ffmpegVm.PreloadSourceAsync(
                                matchedMedia.Url,
                                matchedMedia.DecryptionKey,
                                matchedMedia.Referer
                            );
                        }
                    }
                }
                catch
                {
                    // 预载异常静默忽略，不影响主流程
                }
            },
            token
        );
    }

    private void InitializeNextVideo()
    {
        object? next = FindNextVideo();
        HasNextVideo = next is not null;
        if (next is Episode episode)
        {
            NextVideoTip = string.Format(
                Resources.PlayNextPartTipTemplate,
                $"第 {episode.Number} 集"
            );
        }
        else if (next is Drama rec)
        {
            NextVideoTip = string.Format(Resources.PlayNextVideoTipTemplate, rec.Title);
        }
    }

    private void ReloadPart()
    {
        if (_currentEpisode is null)
        {
            return;
        }

        if (Player.Position > 0)
        {
            _initialProgress = Player.Position;
        }

        InitializeDashMediaCommand.Execute(_currentEpisode);
    }

    private void SyncDownloadAndSubtitle()
    {
        bool isAISubtitleFiltered = _settings.FilterAISubtitle;
        Downloader.HasAvailableSubtitle =
            Subtitle.IsAvailable && (!isAISubtitleFiltered || Subtitle.Metas.Any(p => !p.IsAI));
    }

    private void InitializeLoops()
    {
        VideoLoopType currentType = CurrentLoop;
        List<VideoLoopType> loops = [VideoLoopType.None, VideoLoopType.Single];
        if (_episodes?.Count > 1)
        {
            loops.Add(VideoLoopType.List);
        }

        if (!loops.Contains(CurrentLoop))
        {
            CurrentLoop = VideoLoopType.None;
        }

        LoopTypes = loops;
        CurrentLoop = VideoLoopType.None;
        CurrentLoop = currentType;
    }
}
