using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Contracts.Media;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.ApplicationModel.DataTransfer;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 短剧播放器页面视图模型.
/// </summary>
internal sealed partial class VideoPlayerPageViewModel : PlayerPageViewModelBase
{
    private readonly INotificationService _notificationService;
    private readonly INavigationService _navigationService;
    private readonly PinnerViewModel _pinnerViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<VideoPlayerPageViewModel> _logger;
    private readonly CommentMainViewModel _comments;
    private readonly IDramaService _dramaService;
    private readonly DramaReviewSectionDetailViewModel _dramaReviews;
    private readonly IHistoryService _historyService;
    private readonly IPlaybackMediaService _playbackMediaService;
    private readonly IFavoriteService _favoriteService;
    private readonly ILikeService _likeService;

    private DramaDetails? _dramaDetails;
    private string _dramaId = string.Empty;
    private string _currentVideoId = string.Empty;
    private IReadOnlyList<PlaybackMedia>? _currentMediaList;
    private IReadOnlyList<Episode>? _episodes;
    private Episode? _currentEpisode;

    private CancellationTokenSource? _pageLoadCancellationTokenSource;
    private CancellationTokenSource? _dashLoadCancellationTokenSource;
    private CancellationTokenSource? _preloadCancellationTokenSource;
    private VideoPlayerRecommendSectionDetailViewModel? _recommendSection;
    private string? _preloadedEpisodeVideoId;
    private IReadOnlyList<PlaybackMedia>? _preloadedMediaList;

    private int _initialProgress;
    private bool _isFormatChanging;

    [ObservableProperty]
    public partial bool IsPageLoading { get; set; }

    [ObservableProperty]
    public partial bool IsPageLoadFailed { get; set; }

    [ObservableProperty]
    public partial bool IsMediaLoading { get; set; }

    [ObservableProperty]
    public partial bool IsMediaLoadFailed { get; set; }

    [ObservableProperty]
    public partial Uri? Cover { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PublishRelativeTime { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsFollow { get; set; }

    [ObservableProperty]
    public partial bool IsMyVideo { get; set; }

    [ObservableProperty]
    public partial double PlayerWidth { get; set; }

    [ObservableProperty]
    public partial double PlayerHeight { get; set; }

    [ObservableProperty]
    public partial double PlayCount { get; set; }

    [ObservableProperty]
    public partial double CommentCount { get; set; }

    [ObservableProperty]
    public partial double LikeCount { get; set; }

    [ObservableProperty]
    public partial double FavoriteCount { get; set; }

    [ObservableProperty]
    public partial string OnlineCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLiked { get; set; }

    [ObservableProperty]
    public partial bool IsFavorited { get; set; }

    [ObservableProperty]
    public partial bool HasNextVideo { get; set; }

    [ObservableProperty]
    public partial string NextVideoTip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial VideoLoopType CurrentLoop { get; set; }

    [ObservableProperty]
    public partial PlayerFormatItemViewModel? SelectedFormat { get; set; }

    [ObservableProperty]
    public partial IPlayerSectionDetailViewModel? SelectedSection { get; set; }

    [ObservableProperty]
    public partial List<PlayerFormatItemViewModel>? Formats { get; set; }

    [ObservableProperty]
    public partial List<string>? Tags { get; set; }

    [ObservableProperty]
    public partial List<PlayerFavoriteFolderViewModel>? FavoriteFolders { get; set; }

    [ObservableProperty]
    public partial List<DramaCreditItemViewModel>? Credits { get; set; }

    [ObservableProperty]
    public partial List<VideoLoopType> LoopTypes { get; set; } = [];

    [ObservableProperty]
    public partial List<IPlayerSectionDetailViewModel>? Sections { get; set; }

    /// <summary>
    /// 视图数据加载完成.
    /// </summary>
    public event EventHandler? ViewInitialized;

    /// <summary>
    /// 区块加载完成.
    /// </summary>
    public event EventHandler? SectionInitialized;

    /// <summary>
    /// 弹幕视图模型.
    /// </summary>
    public DanmakuViewModel Danmaku { get; }

    /// <summary>
    /// 字幕视图模型.
    /// </summary>
    public SubtitleViewModel Subtitle { get; }

    /// <summary>
    /// 当前剧集 ID.
    /// </summary>
    public string DramaId => _dramaId;

    /// <summary>
    /// 当前剧集名称.
    /// </summary>
    public string DramaTitle => _dramaDetails?.Drama?.Title ?? Title ?? string.Empty;

    /// <summary>
    /// 当前封面 URL.
    /// </summary>
    public string CoverUrl => _dramaDetails?.Drama?.CoverUrl ?? Cover?.ToString() ?? string.Empty;

    /// <summary>
    /// 当前剧集所有分集.
    /// </summary>
    public IReadOnlyList<Episode>? Episodes => _episodes;

    /// <summary>
    /// 当前正在播放的分集.
    /// </summary>
    public Episode? CurrentEpisode => _currentEpisode;

    public VideoPlayerPageViewModel(
        ILogger<VideoPlayerPageViewModel> logger,
        DanmakuViewModel danmaku,
        SubtitleViewModel subtitle,
        CommentMainViewModel comments,
        ISettingsService settingsService,
        IWindowService windowService,
        INotificationService notificationService,
        INavigationService navigationService,
        PinnerViewModel pinnerViewModel,
        DispatcherQueue dispatcherQueue,
        IDramaService dramaService,
        DramaReviewSectionDetailViewModel dramaReviews,
        IHistoryService historyService,
        IPlaybackMediaService playbackMediaService,
        IFavoriteService favoriteService,
        ILikeService likeService
    )
        : base(settingsService, windowService)
    {
        _notificationService = notificationService;
        _navigationService = navigationService;
        _pinnerViewModel = pinnerViewModel;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
        _comments = comments;
        _dramaService = dramaService;
        _dramaReviews = dramaReviews;
        _historyService = historyService;
        _playbackMediaService = playbackMediaService;
        _favoriteService = favoriteService;
        _likeService = likeService;
        Danmaku = danmaku;
        Subtitle = subtitle;
        CurrentLoop = VideoLoopType.None;
        Player.SetProgressAction(PlayerProgressChanged);
        Player.SetStateAction(PlayerStateChanged);
        Player.SetEndAction(PlayerMediaEnded);
        Player.SetReloadAction(ReloadPart);
        Player.SetSpeedAction(PlayerSpeedChanged);
        Player.SetPlayNextAction(() => _ = PlayNextVideoAsync());
        Player.SetPlayPreviousAction(() => _ = PlayPreviousVideoAsync());

        _favoriteService.FavoriteChanged += OnFavoriteChanged;
        _likeService.LikeChanged += OnLikeChanged;
    }

    private void OnLikeChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            string targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
            if (!string.IsNullOrEmpty(_dramaId) && !string.IsNullOrEmpty(targetVideoId))
            {
                IsLiked = _likeService.IsLiked(_dramaId, targetVideoId);
            }
        });
    }

    private void OnFavoriteChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (!string.IsNullOrEmpty(_dramaId))
            {
                IsFavorited = _favoriteService.IsFavorited(_dramaId);
            }
        });
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(VideoPlayerPage);

    /// <inheritdoc/>
    protected override double GetDefaultNavColumnWidth() => 360d;

    public async Task InitializePageAsync(IPlayableMedia playable)
    {
        if (playable is null || string.IsNullOrWhiteSpace(playable.MediaId))
        {
            return;
        }

        CancelPageLoad();
        CancelDashLoad();
        CancelPreload();

        string dramaId = playable.MediaId;
        string? initialEpisodeId = playable.InitialEpisodeId;

        // 切换不同短剧时，先快照旧剧进度并在后台异步上报，绝不阻塞新剧加载
        if (
            !string.IsNullOrEmpty(_dramaId)
            && _dramaId != dramaId
            && _currentEpisode is not null
            && Player.Position > 0
            && !Player.IsPlayerDataLoading
        )
        {
            _ = ReportProgressSnapshotAsync(
                _dramaId,
                _currentEpisode,
                Player.Position,
                DramaTitle,
                CoverUrl
            );
        }

        Player.ReleaseCurrentSource();
        ClearView();
        Player.CancelNotification();
        Player.IsSeparatorWindowPlayer = IsSeparatorWindowPlayer;

        _pageLoadCancellationTokenSource = new CancellationTokenSource();
        CancellationToken pageToken = _pageLoadCancellationTokenSource.Token;
        _dramaId = dramaId;
        IsPageLoading = true;
        Player.IsPlayerDataLoading = true;

        // 0ms 预填导航入参已有数据并立即渲染首屏，播放器区域同步进入加载态
        ApplyInitialPlayableData(playable);
        LoadInitialProgress(playable.InitialPositionMs);
        InitializeBaseSections(initialEpisodeId);
        InitializeLoops();
        _ = RefreshFavoriteStateAsync(dramaId, pageToken);

        // 若导航已携带起始分集 VideoId（如历史续播/新窗口打开），立刻与详情请求并发解析媒体与弹幕
        bool hasPrelaunchedEpisodeMedia = false;
        if (!string.IsNullOrEmpty(initialEpisodeId))
        {
            hasPrelaunchedEpisodeMedia = true;
            _currentVideoId = initialEpisodeId;
            IsLiked = _likeService.IsLiked(_dramaId, initialEpisodeId);
            Danmaku?.ResetData(_dramaId, initialEpisodeId);
            _ = Danmaku?.LoadDanmakusAsync(0);
            Subtitle?.ResetData(_dramaId, initialEpisodeId);
            _ = Subtitle?.InitializeAsync();
            _ = InitializeMediaByVideoIdAsync(_dramaId, initialEpisodeId, episodeNumber: null);
        }

        // 关联推荐在后台异步加载，绝不阻塞首屏显示与起播
        _recommendSection?.StartBackgroundLoad(pageToken);

        try
        {
            // 获取红果短剧详情与分集
            DramaDetails details = await _dramaService.GetDramaDetailsAsync(
                _dramaId,
                cancellationToken: pageToken
            );

            if (pageToken.IsCancellationRequested || _dramaId != dramaId)
            {
                return;
            }

            _dramaDetails = details;
            _episodes = details.Episodes;

            InitializeDramaView(details);
            Episode initialPart = FindInitialPart(initialEpisodeId)
                ?? throw new Exception("无法找到短剧的分集信息.");
            _currentEpisode = initialPart;

            _dramaReviews.Initialize(_dramaId, details.Drama.Score);
            InsertPartSectionIfNeeded();
            InitializeLoops();

            if (hasPrelaunchedEpisodeMedia && initialPart.VideoId == initialEpisodeId)
            {
                IsLiked = _likeService.IsLiked(_dramaId, initialPart.VideoId);
                InitializeNextVideo();
                UpdateSelectedPartInSection(initialPart);
                Player.InitializeSmtc(
                    Cover?.ToString() ?? string.Empty,
                    $"{Title} - 第{initialPart.Number}集"
                );
                if (_currentMediaList is { Count: > 0 })
                {
                    PreloadNextEpisodeMedia();
                }
            }
            else
            {
                ChangePart(initialPart);
            }

            ViewInitialized?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (ex is not TaskCanceledException && ex is not OperationCanceledException)
            {
                if (_dramaDetails is null)
                {
                    IsPageLoadFailed = true;
                }
                Player.IsPlayerDataLoading = false;
                _logger.ZLogError(ex, $"尝试获取短剧 {dramaId} 详情时失败.");
            }
        }
        finally
        {
            IsPageLoading = false;
        }
    }

    private Task InitializeDashMediaAsync(Episode episode, bool forceRefresh = false) =>
        InitializeMediaByVideoIdAsync(_dramaId, episode.VideoId, episode.Number, forceRefresh);

    private async Task InitializeMediaByVideoIdAsync(
        string dramaId,
        string videoId,
        int? episodeNumber,
        bool forceRefresh = false
    )
    {
        if (string.IsNullOrEmpty(dramaId) || string.IsNullOrEmpty(videoId))
        {
            Player.IsPlayerDataLoading = false;
            return;
        }

        if (IsMediaLoading)
        {
            CancelDashLoad();
        }

        bool isPreloadHit =
            !forceRefresh
            && _preloadedEpisodeVideoId == videoId
            && _preloadedMediaList is { Count: > 0 };
        if (!isPreloadHit)
        {
            IsMediaLoading = true;
        }

        try
        {
            _dashLoadCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _dashLoadCancellationTokenSource.Token;
            _currentVideoId = videoId;

            IReadOnlyList<PlaybackMedia>? mediaList;

            // 优先检查下一集预载缓存
            if (isPreloadHit)
            {
                mediaList = _preloadedMediaList;
                _preloadedEpisodeVideoId = null;
                _preloadedMediaList = null;
            }
            else
            {
                // 解析红果短剧媒体源（统一通过 PlaybackMediaService 调度与缓存）
                mediaList = await _playbackMediaService.ResolveMediaAsync(
                    dramaId,
                    videoId,
                    forceRefresh: forceRefresh,
                    token
                );
            }

            if (token.IsCancellationRequested || _dramaId != dramaId || _currentVideoId != videoId)
            {
                return;
            }

            if (mediaList is null || mediaList.Count == 0)
            {
                IsMediaLoading = false;
                Player.IsPlayerDataLoading = false;
                return;
            }

            _currentMediaList = mediaList;
            InitializePlaybackMedias(mediaList);
            IsMediaLoading = false;
        }
        catch (Exception ex)
        {
            if (ex is not TaskCanceledException && ex is not OperationCanceledException)
            {
                IsMediaLoadFailed = true;
                Player.IsPlayerDataLoading = false;
                _logger.ZLogError(
                    ex,
                    $"尝试获取短剧 {dramaId} 的第 {episodeNumber?.ToString() ?? videoId} 分集媒体时失败."
                );
            }
        }
        finally
        {
            IsMediaLoading = false;
        }
    }

    public void CancelPageLoad()
    {
        if (_pageLoadCancellationTokenSource is not null)
        {
            _pageLoadCancellationTokenSource.Cancel();
            _pageLoadCancellationTokenSource.Dispose();
            _pageLoadCancellationTokenSource = null;
            IsPageLoading = false;
        }
    }

    public void CancelDashLoad()
    {
        if (_dashLoadCancellationTokenSource is not null)
        {
            _dashLoadCancellationTokenSource.Cancel();
            _dashLoadCancellationTokenSource.Dispose();
            _dashLoadCancellationTokenSource = null;
            IsMediaLoading = false;
        }
    }

    private void CancelPreload()
    {
        if (_preloadCancellationTokenSource is not null)
        {
            _preloadCancellationTokenSource.Cancel();
            _preloadCancellationTokenSource.Dispose();
            _preloadCancellationTokenSource = null;
        }
    }

    public async Task ChangeFormatAsync(PlayerFormatItemViewModel vm)
    {
        if (
            vm is null
            || vm == SelectedFormat
            || (
                SelectedFormat is not null
                && vm.Data.Quality == SelectedFormat.Data.Quality
                && vm.PlaybackMedia?.Url == SelectedFormat.PlaybackMedia?.Url
            )
        )
        {
            return;
        }

        _isFormatChanging = true;
        bool isFirstSet = SelectedFormat is null;
        SelectedFormat = vm;
        PlaybackMedia? media = vm.PlaybackMedia ?? _currentMediaList?.FirstOrDefault();
        if (media is null)
        {
            _isFormatChanging = false;
            return;
        }

        string videoUrl = media.Url;

        bool isAutoPlay = !isFirstSet || _settings.ShouldAutoPlay;

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

        await Player.SetPlayDataAsync(
            videoUrl,
            null,
            isAutoPlay,
            _initialProgress < 0 ? 0 : _initialProgress,
            extraOptions: _currentEpisode?.VideoId ?? _currentVideoId
        );

        string smtcTitle = _currentEpisode is not null
            ? $"{Title} - 第{_currentEpisode.Number}集"
            : Title;
        Player.InitializeSmtc(Cover?.ToString() ?? string.Empty, smtcTitle);

        // 重置初始进度，避免影响其它视频
        _initialProgress = 0;
        _isFormatChanging = false;

        // 后台轻量预载下一集，保持低内存且无缝秒切
        PreloadNextEpisodeMedia();
    }

    [RelayCommand]
    public async Task CleanAsync()
    {
        CancelPageLoad();
        CancelDashLoad();
        CancelPreload();

        if (
            !string.IsNullOrEmpty(_dramaId)
            && _currentEpisode is not null
            && Player.Position > 0
            && !Player.IsPlayerDataLoading
        )
        {
            _ = ReportProgressSnapshotAsync(
                _dramaId,
                _currentEpisode,
                Player.Position,
                DramaTitle,
                CoverUrl
            );
        }

        ClearView();
        Subtitle?.ClearAll();
        Danmaku.ClearAll();
        if (Player is not null)
        {
            await Player.CloseAsync();
        }
    }

    /// <summary>
    /// 清理播放器与页面数据 (同步触发).
    /// </summary>
    public void Clean() => _ = CleanAsync();

    public void SelectSection(IPlayerSectionDetailViewModel section)
    {
        if (section is null || section == SelectedSection)
        {
            return;
        }

        SelectedSection = section;
        _ = SelectedSection.TryFirstLoadAsync();
    }

    private Task ReportProgressAsync(int progress)
    {
        if (
            string.IsNullOrEmpty(_dramaId)
            || _currentEpisode is null
            || progress == 0
            || Player.IsPlayerDataLoading
        )
        {
            return Task.CompletedTask;
        }

        return ReportProgressSnapshotAsync(
            _dramaId,
            _currentEpisode,
            progress,
            _dramaDetails?.Drama?.Title ?? Title,
            _dramaDetails?.Drama?.CoverUrl ?? Cover?.ToString() ?? string.Empty
        );
    }

    private async Task ReportProgressSnapshotAsync(
        string dramaId,
        Episode episode,
        int progress,
        string title,
        string coverUrl
    )
    {
        if (
            !_settings.ShouldReportProgress
            || string.IsNullOrEmpty(dramaId)
            || progress <= 0
        )
        {
            return;
        }

        try
        {
            using CancellationTokenSource cancellationToken = new(TimeSpan.FromSeconds(2));
            await _historyService.ReportProgressAsync(
                new ProgressUpdate
                {
                    DramaId = dramaId,
                    VideoId = episode.VideoId,
                    EpisodeNumber = episode.Number,
                    CurrentPositionMs = progress * 1000L,
                    DurationMs = (long)episode.Duration.TotalMilliseconds,
                },
                title: title,
                coverUrl: coverUrl,
                cancellationToken: cancellationToken.Token
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试上报短剧播放进度时失败.");
        }
    }

    partial void OnPlayerWidthChanged(double value) => CalcPlayerHeight();

    partial void OnCurrentLoopChanged(VideoLoopType value) => InitializeNextVideo();

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        try
        {
            string targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
            bool state = !IsLiked;
            if (!string.IsNullOrEmpty(_dramaId) && !string.IsNullOrEmpty(targetVideoId))
            {
                PhoenixOperationResult res = await _likeService.SetDramaEpisodeLikeAsync(
                    _dramaId,
                    targetVideoId,
                    state,
                    drama: _dramaDetails?.Drama
                );
                if (res.Succeeded)
                {
                    IsLiked = state;
                    LikeCount += state ? 1 : -1;
                }
                else
                {
                    _notificationService.ShowInAppNotification(
                        res.Message ?? "点赞操作失败",
                        NotificationType.Warning
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
            if (string.IsNullOrEmpty(_dramaId))
            {
                return;
            }

            bool targetState = !IsFavorited;
            bool success = await _favoriteService.SetFavoriteAsync(
                _dramaId,
                targetState,
                _dramaDetails?.Drama
            );

            if (success)
            {
                IsFavorited = targetState;
                FavoriteCount += targetState ? 1 : -1;
                string tip = targetState
                    ? Resources.AddFavoriteSucceed
                    : Resources.CancelFavoriteSucceed;
                _notificationService.ShowInAppNotification(tip, NotificationType.Success);
            }
            else
            {
                _notificationService.ShowInAppNotification(
                    Resources.FavoriteFailed,
                    NotificationType.Warning
                );
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试收藏/取消收藏剧集时失败.");
            _notificationService.ShowInAppNotification(
                Resources.FavoriteFailed,
                NotificationType.Error
            );
        }
    }

    [RelayCommand]
    private async Task ShareAsync()
    {
        try
        {
            string targetVideoId = _currentEpisode?.VideoId ?? _currentVideoId;
            ShareLink shareInfo = await _dramaService.CreateShareLinkAsync(_dramaId, targetVideoId);
            string url = !string.IsNullOrEmpty(shareInfo.Url) ? shareInfo.Url : shareInfo.ShortUrl;

            DataPackage package = new();
            package.SetText(url);
            package.SetWebLink(new Uri(url));
            Clipboard.SetContent(package);
            _notificationService.ShowInAppNotification(Resources.Copied, NotificationType.Success);
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
            if (!string.IsNullOrEmpty(_dramaId))
            {
                bool success = await _favoriteService.SetFavoriteAsync(
                    _dramaId,
                    true,
                    _dramaDetails?.Drama
                );
                if (success)
                {
                    IsFavorited = true;
                    _notificationService.ShowInAppNotification(
                        Resources.AddFavoriteSucceed,
                        NotificationType.Success
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"加入追剧列表失败");
            _notificationService.ShowInAppNotification(
                Resources.FavoriteFailed,
                NotificationType.Error
            );
        }
    }

    [RelayCommand]
    public async Task PlayNextVideoAsync()
    {
        if (_episodes is null || Player.IsPlayerDataLoading)
        {
            return;
        }

        object? nextPart = FindNextVideo();
        if (nextPart is null)
        {
            _notificationService.ShowInAppNotification(
                "已经是最后一集了",
                NotificationType.Information
            );
            Player.BackToDefaultMode();
            return;
        }

        _initialProgress = 0;

        if (nextPart is Episode episode)
        {
            ChangePart(episode);
        }
        else if (nextPart is Drama rec)
        {
            _ = InitializePageAsync(
                new PlayableMedia(rec.Id, rec.Title, rec.CoverUrl, Drama: rec)
            );
        }
        else
        {
            Player.BackToDefaultMode();
        }
    }

    public async Task PlayPreviousVideoAsync()
    {
        if (_episodes is null || Player.IsPlayerDataLoading)
        {
            return;
        }

        Episode? prevPart = FindPreviousVideo();
        if (prevPart is null)
        {
            _notificationService.ShowInAppNotification(
                "已经是第一集了",
                NotificationType.Information
            );
            return;
        }

        _initialProgress = 0;
        ChangePart(prevPart);
    }

    [RelayCommand]
    private void OpenInNewWindow()
    {
        if (Player.Position > 0)
        {
            _ = ReportProgressAsync(Player.Position);
        }

        if (!Player.IsPaused)
        {
            Player.TogglePlayPause();
        }

        new PlayerWindow().OpenPlayable(
            new PlayableMedia(
                _dramaId,
                DramaTitle,
                CoverUrl,
                InitialEpisodeId: _currentEpisode?.VideoId,
                InitialPositionMs: Player.Position > 0 ? Player.Position * 1000L : null,
                Drama: _dramaDetails?.Drama
            )
        );
    }

    [RelayCommand]
    private void Pin()
    {
        PinItem pinItem = new(_dramaId, Title, Cover?.ToString() ?? string.Empty, PinType.Drama);
        _pinnerViewModel.AddItem(pinItem);
    }

    [RelayCommand]
    private void CopyTitle()
    {
        DataPackage dp = new();
        dp.SetText(Title);
        Clipboard.SetContent(dp);
        _notificationService.ShowInAppNotification(Resources.Copied, NotificationType.Success);
    }

    private void ChangePart(Episode episode)
    {
        if (episode is null)
        {
            return;
        }

        // 1. 抓取当前帧并锁定呈现到 SwapChainPanel 上（无闪黑平滑过渡）
        if (Player is FFmpegPlayerViewModel ffmpegVm)
        {
            byte[]? framePixels = ffmpegVm.CaptureCurrentFrame(out int frameW, out int frameH);
            if (framePixels is not null && frameW > 0 && frameH > 0)
            {
                Danmaku?.ShowFrozenFrame(framePixels, frameW, frameH);
            }
        }

        // 2. 在发起网络请求前立即释放旧源，确保旧集瞬间停止播放，避免漏声音与画面
        Player.ReleaseCurrentSource();

        if (_initialProgress == 0)
        {
            _initialProgress = -1;
        }

        _currentEpisode = episode;
        _currentVideoId = episode.VideoId;
        IsLiked = _likeService.IsLiked(_dramaId, episode.VideoId);
        Player.CancelNotification();

        Danmaku?.ResetData(_dramaId, episode.VideoId);
        var episodeDuration = (int)episode.Duration.TotalSeconds;
        _ = Danmaku?.LoadDanmakusAsync(episodeDuration);
        Subtitle?.ResetData(_dramaId, episode.VideoId);

        // 加载分集媒体
        _ = InitializeDashMediaAsync(episode);
        _ = Subtitle?.InitializeAsync();

        // 切换分集评论
        _comments.Initialize(_dramaId, episode.VideoId, CommentSortOrder.Hot);
        if (SelectedSection == _comments)
        {
            _ = _comments.TryFirstLoadAsync();
        }

        InitializeNextVideo();
        VideoPlayerPartSectionDetailViewModel? partSection =
            Sections?.FirstOrDefault(p => p is VideoPlayerPartSectionDetailViewModel)
            as VideoPlayerPartSectionDetailViewModel;
        partSection?.UpdateSelectedPart(episode);
    }

    private void PlayerProgressChanged(int progress, int duration)
    {
        // 兜底静默尝试：若当前分集尚未成功加载弹幕且未在加载中，发起一次加载尝试
        if (
            Danmaku is not null
            && Danmaku is { HasLoaded: false, IsLoading: false }
            && !string.IsNullOrEmpty(_dramaId)
            && !string.IsNullOrEmpty(_currentVideoId)
        )
        {
            _ = Danmaku.LoadDanmakusAsync(duration);
        }

        Danmaku?.UpdatePosition(progress);
        Subtitle?.UpdatePosition(progress);
    }

    private void PlayerStateChanged(PlayerState state)
    {
        if (_currentEpisode is null && string.IsNullOrEmpty(_dramaId))
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
                _ = ReportProgressAsync(Player.Position);
            }
            else if (state == PlayerState.Failed && _currentEpisode is not null)
            {
                // 播放失败/地址失效时使当前分集媒体缓存失效，便于重试时刷新覆盖
                _ = _playbackMediaService.InvalidateCacheAsync(_dramaId, _currentEpisode.VideoId);
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
            // 抓取播完时的最后一帧并呈现到 SwapChainPanel 上，防止播放完毕切集或循环时闪黑
            if (Player is FFmpegPlayerViewModel ffmpegVm)
            {
                byte[]? framePixels = ffmpegVm.CaptureCurrentFrame(out int frameW, out int frameH);
                if (framePixels is not null && frameW > 0 && frameH > 0)
                {
                    Danmaku?.ShowFrozenFrame(framePixels, frameW, frameH);
                }
            }

            // 清除字幕（注意：不清除弹幕数据列表，以便单集循环或手动重播时仍能正常呈现弹幕）.
            Subtitle.ClearSubtitle();

            _ = ReportProgressAsync(Player.Duration);

            // 单视频循环.
            if (CurrentLoop == VideoLoopType.Single)
            {
                Danmaku?.UpdatePosition(0);
                Player.Restart();
                return;
            }

            object? next = FindNextVideo();
            if (next is null)
            {
                if (!_isFormatChanging)
                {
                    Player.BackToDefaultMode();
                }
                return;
            }

            // 判断下一项类型与对应自动播放设置
            bool shouldAutoPlay = false;
            if (next is Episode)
            {
                shouldAutoPlay = _settings.AutoPlayNextEpisode;
            }
            else if (next is Drama)
            {
                shouldAutoPlay = _settings.AutoPlayNextRecommendDrama;
            }

            if (!shouldAutoPlay)
            {
                if (!_isFormatChanging)
                {
                    Player.BackToDefaultMode();
                }
                return;
            }

            if (_episodes is null || Player.IsPlayerDataLoading)
            {
                return;
            }

            // 直接播放下一集或下一个短剧，无需触发倒计时浮层弹窗
            _ = PlayNextVideoAsync();
        });
    }

    private void ApplyInitialPlayableData(IPlayableMedia playable)
    {
        if (playable.Drama is Drama drama)
        {
            InitializeDramaView(drama);
            return;
        }

        Title = playable.Title;
        Cover =
            !string.IsNullOrEmpty(playable.CoverUrl)
            && Uri.TryCreate(playable.CoverUrl, UriKind.Absolute, out Uri? coverUri)
                ? coverUri
                : null;
        Player.Title = Title;
        CalcPlayerHeight();
    }

    private async Task RefreshFavoriteStateAsync(string dramaId, CancellationToken cancellationToken)
    {
        try
        {
            bool favorited = await _favoriteService.IsFavoritedAsync(
                dramaId,
                cancellationToken: cancellationToken
            );
            if (!cancellationToken.IsCancellationRequested && _dramaId == dramaId)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (_dramaId == dramaId)
                    {
                        IsFavorited = favorited;
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
    }

    private void InitializeDramaView(DramaDetails details) => InitializeDramaView(details.Drama);

    private void InitializeDramaView(Drama drama)
    {
        _dramaId = drama.Id;
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
        LikeCount = drama.LikeCount;
        FavoriteCount = drama.FollowCount;
        OnlineCountText = !string.IsNullOrEmpty(drama.Heat)
            ? drama.Heat
            : (!string.IsNullOrEmpty(drama.Score) ? $"{drama.Score} 分" : "热播中");

        PublishRelativeTime = !string.IsNullOrEmpty(drama.OnlineDate)
            ? drama.OnlineDate
            : "红果独播";

        Credits = drama
            .Credits?.Select(c => new DramaCreditItemViewModel(c, _notificationService))
            .ToList()
            ?? [];
        Tags = drama.Tags?.ToList() ?? [];

        Player.Title = Title;
        CalcPlayerHeight();
    }

    private void InitializePlaybackMedias(IReadOnlyList<PlaybackMedia> mediaList)
    {
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

        if (selectedFormat is not null)
        {
            _ = ChangeFormatAsync(selectedFormat);
        }
    }

    private void InitializeBaseSections(string? initialEpisodeId)
    {
        _dramaReviews.Initialize(_dramaId, string.Empty);
        if (!string.IsNullOrEmpty(initialEpisodeId))
        {
            _comments.Initialize(_dramaId, initialEpisodeId, CommentSortOrder.Hot);
        }

        _recommendSection = new VideoPlayerRecommendSectionDetailViewModel(
            _dramaService,
            _dispatcherQueue,
            _logger
        );
        _recommendSection.Initialize(_dramaId);

        List<IPlayerSectionDetailViewModel> sections =
        [
            new VideoPlayerInfoSectionDetailViewModel(this),
            _dramaReviews,
            _comments,
            _recommendSection,
        ];

        Sections = sections;
        SelectSection(Sections.First());
        InitializeNextVideo();
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    private void InsertPartSectionIfNeeded()
    {
        if (
            Sections is null
            || _episodes is not { Count: > 1 }
            || Sections.OfType<VideoPlayerPartSectionDetailViewModel>().Any()
        )
        {
            return;
        }

        VideoPlayerPartSectionDetailViewModel partSection = new(
            _episodes,
            _currentEpisode?.VideoId ?? string.Empty,
            ChangePart,
            _settingsService
        );
        Sections.Insert(1, partSection);
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateSelectedPartInSection(Episode episode)
    {
        VideoPlayerPartSectionDetailViewModel? partSection =
            Sections?.FirstOrDefault(p => p is VideoPlayerPartSectionDetailViewModel)
            as VideoPlayerPartSectionDetailViewModel;
        partSection?.UpdateSelectedPart(episode);
    }

    private void ClearView()
    {
        IsPageLoadFailed = false;
        _dramaId = string.Empty;
        _currentVideoId = string.Empty;
        _episodes = null;
        _currentEpisode = null;
        _dramaDetails = null;
        _currentMediaList = null;
        _preloadedEpisodeVideoId = null;
        _preloadedMediaList = null;
        _recommendSection = null;
        _initialProgress = -1;
        Title = string.Empty;
        Description = string.Empty;
        Cover = null;
        OnlineCountText = string.Empty;
        PublishRelativeTime = string.Empty;
        Credits = null;
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
        Formats = null;
        SelectedSection = null;
        Sections?.Clear();
        Sections = null;
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

    private void LoadInitialProgress(long? initialPositionMs = null)
    {
        _initialProgress = initialPositionMs is > 0
            ? (int)(initialPositionMs.Value / 1000L)
            : 0;
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
        bool isAutoPlayRecommendVideo = _settings.AutoPlayNextRecommendDrama;
        if (
            isAutoPlayRecommendVideo
            && Sections?.OfType<VideoPlayerRecommendSectionDetailViewModel>().FirstOrDefault()
                is VideoPlayerRecommendSectionDetailViewModel recommendSection
        )
        {
            DramaItemViewModel? firstItem = recommendSection.Items.FirstOrDefault();
            return firstItem?.Drama;
        }

        return null;
    }

    private Episode? FindPreviousVideo()
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
            || string.IsNullOrEmpty(_dramaId)
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

                    IReadOnlyList<PlaybackMedia> mediaList =
                        await _playbackMediaService.ResolveMediaAsync(
                            _dramaId,
                            nextEpisode.VideoId,
                            forceRefresh: false,
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

        _ = InitializeDashMediaAsync(_currentEpisode, forceRefresh: true);
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