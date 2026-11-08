using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 短剧播放器页面视图模型.
/// </summary>
public sealed partial class VideoPlayerPageViewModel : PlayerPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerPageViewModel"/> class.
    /// </summary>
    public VideoPlayerPageViewModel(
        ILogger<VideoPlayerPageViewModel> logger,
        DanmakuViewModel danmaku,
        SubtitleViewModel subtitle,
        CommentMainViewModel comments,
        DownloadViewModel download,
        ISettingsService settingsService,
        AppViewModel appViewModel,
        NavigationViewModel navigationViewModel,
        PinnerViewModel pinnerViewModel,
        DispatcherQueue dispatcherQueue,
        PhoenixReadApiClient apiClient,
        DramaReviewSectionDetailViewModel dramaReviews
    )
        : base(settingsService, appViewModel)
    {
        _navigationViewModel = navigationViewModel;
        _pinnerViewModel = pinnerViewModel;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
        _comments = comments;
        _apiClient = apiClient;
        _dramaReviews = dramaReviews;
        Danmaku = danmaku;
        Subtitle = subtitle;
        Downloader = download;
        CurrentLoop = VideoLoopType.None;
        Subtitle.SetInitializedCallback(SyncDownloadAndSubtitle);
        Player.SetProgressAction(PlayerProgressChanged);
        Player.SetStateAction(PlayerStateChanged);
        Player.SetEndAction(PlayerMediaEnded);
        Player.SetReloadAction(ReloadPart);
        Player.SetSpeedAction(PlayerSpeedChanged);
        Player.SetPlayNextAction(() => PlayNextVideoCommand.Execute(default));
        Player.SetPlayPreviousAction(() => PlayPreviousVideoCommand.Execute(default));
    }

    /// <summary>
    /// 注入播放列表.
    /// </summary>
    public void InjectPlaylist(IList<Drama> playlist) => _dramaPlaylist = playlist;

    /// <summary>
    /// 注入播放列表 (兼容老调用).
    /// </summary>
    public void InjectPlaylist(object? playlist) { }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(VideoPlayerPage);

    /// <inheritdoc/>
    protected override double GetDefaultNavColumnWidth() => 360d;

    [RelayCommand]
    private async Task InitializePageAsync(object parameter)
    {
        if (IsPageLoading)
        {
            CancelPageLoad();
        }

        IsPageLoading = true;
        string seriesId = string.Empty;

        if (parameter is VideoSnapshot snapshot)
        {
            seriesId = snapshot.Video.Identifier.Id;
            IsPrivatePlay = snapshot.IsPrivate;
        }
        else if (parameter is Drama drama)
        {
            seriesId = drama.Id;
        }
        else if (parameter is string id)
        {
            seriesId = id;
        }
        else if (parameter is VideoItemViewModel videoVm)
        {
            seriesId = videoVm.Drama?.Id ?? videoVm.Data.Identifier.Id;
        }
        else if (parameter is SeasonItemViewModel seasonVm)
        {
            seriesId = seasonVm.Drama?.Id ?? seasonVm.Data.Identifier.Id;
        }

        try
        {
            if (!string.IsNullOrEmpty(_seriesId) && _seriesId != seriesId)
            {
                // 记录上一个视频的播放进度.
                await ReportProgressAsync(Player.Position);
            }

            ClearView();
            Player.CancelNotification();

            Player.IsSeparatorWindowPlayer = IsSeparatorWindowPlayer;
            _pageLoadCancellationTokenSource = new CancellationTokenSource();
            _seriesId = seriesId;

            // 获取红果短剧详情与分集
            var details = await _apiClient.GetDramaDetailsAsync(
                _seriesId,
                _pageLoadCancellationTokenSource.Token
            );
            _dramaDetails = details;
            _episodes = details.Episodes;

            InitializeDramaView(details);
            var initialPart =
                FindInitialPart(default) ?? throw new Exception("无法找到短剧的分集信息.");
            _currentEpisode = initialPart;

            // 初始化整剧剧评与分集评论
            _dramaReviews.Initialize(_seriesId, details.Drama.Score);
            _comments.Initialize(_seriesId, _currentEpisode.VideoId, CommentSortOrder.Hot);

            LoadInitialProgress();
            await InitializeSectionsAsync();
            InitializeLoops();
            ChangePart(initialPart);
            ViewInitialized?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (ex is not TaskCanceledException)
            {
                IsPageLoadFailed = true;
                _logger.LogError(ex, $"尝试获取短剧 {seriesId} 详情时失败.");
            }
            else
            {
                return;
            }
        }
        finally
        {
            IsPageLoading = false;
        }
    }

    [RelayCommand]
    private async Task InitializeDashMediaAsync(Episode episode)
    {
        if (IsMediaLoading)
        {
            CancelDashLoad();
        }

        var isPreloadHit = _preloadedEpisodeVideoId == episode.VideoId && _preloadedMediaList is { Count: > 0 };
        if (!isPreloadHit)
        {
            IsMediaLoading = true;
        }

        try
        {
            _dashLoadCancellationTokenSource = new CancellationTokenSource();
            _currentVideoId = episode.VideoId;

            IReadOnlyList<PlaybackMedia>? mediaList = null;

            // 优先检查下一集预载缓存
            if (isPreloadHit)
            {
                mediaList = _preloadedMediaList;
                _preloadedEpisodeVideoId = null;
                _preloadedMediaList = null;
            }
            else
            {
                // 解析红果短剧媒体源（包含各清晰度与 AES-128 CENC 解密密钥）
                mediaList = await _apiClient.ResolveMediaAsync(
                    _seriesId,
                    episode.VideoId,
                    _dashLoadCancellationTokenSource.Token
                );
            }

            if (mediaList is null || mediaList.Count == 0)
            {
                IsMediaLoading = false;
                return;
            }

            _currentMediaList = mediaList;
            InitializePlaybackMedias(mediaList);
            IsMediaLoading = false;
        }
        catch (Exception ex)
        {
            if (ex is not TaskCanceledException)
            {
                IsMediaLoadFailed = true;
                _logger.LogError(
                    ex,
                    $"尝试获取短剧 {_seriesId} 的第 {episode.Number} 分集媒体时失败."
                );
            }
            else
            {
                return;
            }
        }
        finally
        {
            IsMediaLoading = false;
        }
    }

    [RelayCommand]
    private void CancelPageLoad()
    {
        if (_pageLoadCancellationTokenSource is not null)
        {
            _pageLoadCancellationTokenSource.Cancel();
            _pageLoadCancellationTokenSource.Dispose();
            _pageLoadCancellationTokenSource = null;
            IsPageLoading = false;
        }
    }

    [RelayCommand]
    private void CancelDashLoad()
    {
        if (_dashLoadCancellationTokenSource is not null)
        {
            _dashLoadCancellationTokenSource.Cancel();
            _dashLoadCancellationTokenSource.Dispose();
            _dashLoadCancellationTokenSource = null;
            IsMediaLoading = false;
        }
    }

    [RelayCommand]
    private async Task ChangeFormatAsync(PlayerFormatItemViewModel vm)
    {
        if (vm is null || vm == SelectedFormat)
        {
            return;
        }

        _isFormatChanging = true;
        var isFirstSet = SelectedFormat == default;
        SelectedFormat = vm;
        var media = vm.PlaybackMedia ?? _currentMediaList?.FirstOrDefault();
        if (media == null)
        {
            _isFormatChanging = false;
            return;
        }

        var videoUrl = media.Url;

        var isAutoPlay = !isFirstSet || _settings.ShouldAutoPlay;

        // 切换清晰度时保持当前播放进度
        if (_initialProgress == 0 && Player.Position != 0)
        {
            _initialProgress = Player.Position;
        }

        _settings.LastSelectedVideoQuality = vm.Data.Quality;

        // 注入 CENC 解密密钥与 Referer 请求头
        Player.DecryptionKey = media.DecryptionKey;
        Player.CustomReferer = !string.IsNullOrEmpty(media.Referer)
            ? media.Referer
            : "https://novel.snssdk.com";

        Danmaku.ClearAll();
        await Player.SetPlayDataAsync(
            videoUrl,
            null,
            isAutoPlay,
            _initialProgress < 0 ? 0 : _initialProgress,
            extraOptions: _currentEpisode?.VideoId
        );

        Player.InitializeSmtc(
            Cover?.ToString() ?? string.Empty,
            $"{Title} - 第{_currentEpisode?.Number}集"
        );

        // 重置初始进度，避免影响其它视频
        _initialProgress = 0;
        _isFormatChanging = false;

        // 后台轻量预载下一集，保持低内存且无缝秒切
        PreloadNextEpisodeMedia();
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        await ReportProgressAsync(Player.Position);
        ClearView();
        Subtitle?.ClearAll();
        Danmaku.ClearAll();
        await Player?.CloseAsync();
    }

    [RelayCommand]
    private void SelectSection(IPlayerSectionDetailViewModel section)
    {
        if (section is null || section == SelectedSection)
        {
            return;
        }

        SelectedSection = section;
        SelectedSection.TryFirstLoadCommand.Execute(default);
    }

    [RelayCommand]
    private async Task ReportProgressAsync(int progress)
    {
        var shouldReport = _settings.ShouldReportProgress;
        if (
            !shouldReport
            || IsPrivatePlay
            || string.IsNullOrEmpty(_seriesId)
            || _currentEpisode is null
            || progress == 0
            || Player.IsPlayerDataLoading
        )
        {
            return;
        }

        try
        {
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _apiClient.ReportProgressAsync(
                new ProgressUpdate
                {
                    SeriesId = _seriesId,
                    VideoId = _currentEpisode.VideoId,
                    EpisodeNumber = _currentEpisode.Number,
                    CurrentPositionMs = progress * 1000L,
                    DurationMs = (long)_currentEpisode.Duration.TotalMilliseconds,
                },
                cancellationToken.Token
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试上报短剧播放进度时失败.");
        }
    }

    partial void OnPlayerWidthChanged(double value) => CalcPlayerHeight();

    partial void OnCurrentLoopChanged(VideoLoopType value) => InitializeNextVideo();
}
