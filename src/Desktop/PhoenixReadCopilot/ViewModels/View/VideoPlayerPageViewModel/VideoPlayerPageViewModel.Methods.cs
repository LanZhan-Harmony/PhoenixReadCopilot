using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 短剧播放器页面视图模型.
/// </summary>
public sealed partial class VideoPlayerPageViewModel
{
    private void InitializeDramaView(DramaDetails details)
    {
        var drama = details.Drama;
        _seriesId = drama.Id;
        Title = drama.Title;
        Description = drama.Description;
        Cover =
            !string.IsNullOrEmpty(drama.CoverUrl)
            && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out var coverUri)
                ? coverUri
                : default;
        PlayCount =
            drama.PlayCount > 0
                ? drama.PlayCount
                : (double.TryParse(drama.Views, out var vc) ? vc : 0);
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
        Downloader.InitializeDrama(_seriesId, Title, Cover?.ToString() ?? string.Empty);

        // 按清晰度文案去重，每种清晰度只保留一条，优先保留带解密密钥与高码率的
        var distinctMedias = mediaList
            .GroupBy(PlayerFormatItemViewModel.GetFormatKey)
            .Select(g =>
                g.OrderByDescending(m => !string.IsNullOrEmpty(m.DecryptionKey))
                    .ThenByDescending(m => m.Bitrate)
                    .First()
            )
            .OrderByDescending(PlayerFormatItemViewModel.GetQualityScore)
            .ToList();

        Formats = distinctMedias.Select(m => new PlayerFormatItemViewModel(m)).ToList();

        var partIndex =
            _episodes is not null && _currentEpisode is not null
                ? _episodes.ToList().FindIndex(e => e.VideoId == _currentEpisode.VideoId)
                : 0;

        Downloader.InitializeMetas(
            GetWebLink(),
            Formats.Select(p => p.Data).ToList(),
            default,
            partIndex >= 0 ? partIndex + 1 : 1
        );

        // 选择最佳画质或用户偏好画质
        var selectedFormat = Formats.FirstOrDefault();
        var lastSelectedQuality = _settings.LastSelectedVideoQuality;
        if (lastSelectedQuality > 0)
        {
            var matched = Formats.Find(p => p.Data.Quality == lastSelectedQuality);
            if (matched != null)
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

        var sections = new List<IPlayerSectionDetailViewModel>
        {
            new VideoPlayerInfoSectionDetailViewModel(this),
        };

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
            var rec = await _apiClient.GetSeriesRecommendationsAsync(_seriesId);
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
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    private void ClearView()
    {
        IsPageLoadFailed = false;
        _episodes = default;
        _currentEpisode = default;
        _dramaDetails = default;
        _initialProgress = -1;
        Tags = default;
        IsFollow = false;
        IsMyVideo = false;
        PlayCount = 0;
        DanmakuCount = 0;
        CommentCount = 0;
        LikeCount = 0;
        CoinCount = 0;
        FavoriteCount = 0;
        IsLiked = false;
        IsFavorited = false;
        FavoriteFolders = default;
        SelectedFormat = default;
        Sections?.Clear();
    }

    private void CalcPlayerHeight()
    {
        if (PlayerWidth <= 0 || Player.IsFullScreen || Player.IsCompactOverlay)
        {
            return;
        }

        // 红果短剧默认为 9:16 竖屏短剧
        var ratioWidth = 9.0;
        var ratioHeight = 16.0;
        if (_currentEpisode?.IsVertical == false)
        {
            ratioWidth = 16.0;
            ratioHeight = 9.0;
        }

        PlayerHeight = PlayerWidth * ratioHeight / ratioWidth;
    }

    private Episode? FindInitialPart(string? initialVideoId = default)
    {
        if (_episodes?.Count == 1)
        {
            return _episodes.First();
        }

        Episode? part = default;
        if (!string.IsNullOrEmpty(initialVideoId) && _episodes is not null)
        {
            part = _episodes.FirstOrDefault(p => p.VideoId == initialVideoId);
        }

        return part ?? _episodes?.FirstOrDefault();
    }

    private void LoadInitialProgress(int? progress = default)
    {
        _initialProgress = 0;
        if (progress is not null)
        {
            _initialProgress = progress.Value;
        }
    }

    private object? FindNextVideo()
    {
        var isListLoop = CurrentLoop == VideoLoopType.List;

        // 1. 先检查分集列表中是否有下一集.
        if (_episodes is not null && _currentEpisode is not null)
        {
            var list = _episodes.ToList();
            var index = list.FindIndex(e => e.VideoId == _currentEpisode.VideoId);
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
        var isAutoPlayRecommendVideo = _settings.AutoPlayNextRecommendVideo;
        if (
            isAutoPlayRecommendVideo
            && Sections?.OfType<VideoPlayerRecommendSectionDetailViewModel>().FirstOrDefault()
                is VideoPlayerRecommendSectionDetailViewModel recommendSection
        )
        {
            return recommendSection.Items.FirstOrDefault()?.Data;
        }

        return default;
    }

    private void InitializeNextVideo()
    {
        if (Sections is null || Sections.Count == 0)
        {
            return;
        }

        var next = FindNextVideo();
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
        var isAISubtitleFiltered = _settings.FilterAISubtitle;
        Downloader.HasAvailableSubtitle =
            Subtitle.IsAvailable
            && (isAISubtitleFiltered ? Subtitle.Metas.Any(p => !p.IsAI) : true);
    }

    private void InitializeLoops()
    {
        var currentType = CurrentLoop;
        var loops = new List<VideoLoopType>
        {
            VideoLoopType.None,
            VideoLoopType.Single
        };
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
