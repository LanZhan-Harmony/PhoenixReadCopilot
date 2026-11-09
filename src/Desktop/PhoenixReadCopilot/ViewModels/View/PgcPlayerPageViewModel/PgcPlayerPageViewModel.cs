using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Toolkits;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.Media;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// PGC 播放器页面视图模型.
/// </summary>
public sealed partial class PgcPlayerPageViewModel : PlayerPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PgcPlayerPageViewModel"/> class.
    /// </summary>
    public PgcPlayerPageViewModel(
        IPlayerService service,
        IFavoriteService favoriteService,
        IEntertainmentDiscoveryService discoveryService,
        ILogger<PgcPlayerPageViewModel> logger,
        DanmakuViewModel danmaku,
        SubtitleViewModel subtitle,
        CommentMainViewModel comments,
        DownloadViewModel downloader,
        ISettingsService settingsService,
        AppViewModel appViewModel,
        NavigationViewModel navigationViewModel,
        PinnerViewModel pinnerViewModel,
        DispatcherQueue dispatcherQueue
    )
        : base(settingsService, appViewModel)
    {
        _navigationViewModel = navigationViewModel;
        _pinnerViewModel = pinnerViewModel;
        _dispatcherQueue = dispatcherQueue;
        _service = service;
        _favoriteService = favoriteService;
        _discoveryService = discoveryService;
        _comments = comments;
        _logger = logger;
        Danmaku = danmaku;
        Subtitle = subtitle;
        Downloader = downloader;
        Player.SetProgressAction(PlayerProgressChanged);
        Player.SetStateAction(PlayerStateChanged);
        Player.SetEndAction(PlayerMediaEnded);
        Player.SetReloadAction(ReloadEpisode);
        Player.SetSpeedAction(PlayerSpeedChanged);
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(PgcPlayerPage);

    /// <inheritdoc/>
    protected override double GetDefaultNavColumnWidth() => 360d;

    [RelayCommand]
    private async Task InitializePageAsync(MediaIdentifier identifier)
    {
        if (IsPageLoading)
        {
            CancelPageLoad();
        }

        IsPageLoading = true;
        try
        {
            ClearView();
            Player.CancelNotification();
            _pageLoadCancellationTokenSource = new CancellationTokenSource();
            Player.IsSeparatorWindowPlayer = IsSeparatorWindowPlayer;
            string id = identifier.Id;
            bool isEpisode = id.StartsWith("ep");
            id = id[3..];
            string? seasonId = isEpisode ? null : id;
            string? episodeId = isEpisode ? id : null;
            PgcPlayerView view = await _service.GetPgcPageDetailAsync(
                seasonId,
                episodeId,
                cancellationToken: _pageLoadCancellationTokenSource.Token
            );
            InitializeView(view);
            EpisodeInformation? initialEpisode = FindInitialEpisode(episodeId);
            ChangeEpisode(initialEpisode);
            InitializeSections();
            InitializeNextEpisode();
            ViewInitialized?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            if (ex is not TaskCanceledException)
            {
                IsPageLoadFailed = true;
                _logger.LogError(ex, $"尝试获取剧集 {identifier.Id} 详情时失败.");
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
    private async Task InitializeDashMediaAsync(EpisodeInformation episode)
    {
        if (IsMediaLoading)
        {
            CancelDashLoad();
        }

        IsMediaLoading = true;
        try
        {
            _dashLoadCancellationTokenSource = new CancellationTokenSource();
            long cid = episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Cid);
            string? seasonType = episode.GetExtensionIfNotNull<string>(
                EpisodeExtensionDataId.SeasonType
            );
            DashMediaInformation info = await _service.GetPgcPlayDetailAsync(
                cid.ToString(),
                episode.Identifier.Id,
                seasonType,
                _dashLoadCancellationTokenSource.Token
            );
            InitializeDash(info);
        }
        catch (Exception ex)
        {
            if (ex is not TaskCanceledException)
            {
                IsMediaLoadFailed = true;
                _logger.LogError(
                    ex,
                    $"尝试获取视频 {_view.Information.Identifier.Id} 的第 {episode.Index} 分集时失败."
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
        bool isFirstSet = SelectedFormat == null;
        SelectedFormat = vm;
        int? maxAudioQuality = _audioSegments?.Max(p => Convert.ToInt32(p.Id));
        string preferCodec = AppToolkit.GetPreferCodecId();
        DashSegmentInformation? vSeg =
            _videoSegments?.FirstOrDefault(p =>
                p.Id == vm.Data.Quality.ToString() && p.Codecs.Contains(preferCodec)
            )
            ?? _videoSegments?.FirstOrDefault(p => p.Id == vm.Data.Quality.ToString());
        DashSegmentInformation? aSeg = _audioSegments?.FirstOrDefault(p =>
            p.Id == maxAudioQuality.ToString()
        );
        string? videoUrl = vSeg?.BaseUrl;
        string? audioUrl = aSeg?.BaseUrl;

        bool isAutoPlay = !isFirstSet;
        if (isFirstSet)
        {
            isAutoPlay = _settings.ShouldAutoPlay;
        }

        // 切换清晰度时，如果播放器已经加载了媒体，那么就保持当前的播放进度.
        if (_initialProgress == 0 && Player.Position != 0)
        {
            _initialProgress = Player.Position;
        }

        _settings.LastSelectedPgcQuality = vm.Data.Quality;
        if (Player is FFmpegPlayerViewModel fpvm)
        {
            // fpvm.InjectSegments(vSeg, aSeg, Convert.ToInt32(_episode?.Duration ?? 0));
        }

        Danmaku.ClearAll();
        await Player.SetPlayDataAsync(
            videoUrl,
            audioUrl,
            isAutoPlay,
            _initialProgress < 0 ? 0 : _initialProgress,
            extraOptions: _episode
                .GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Cid)
                .ToString()
        );
        Player.InitializeSmtc(_episode.Identifier.Cover.SourceUri.ToString(), EpisodeTitle);

        // 重置初始进度，避免影响其它视频.
        _initialProgress = 0;
        _isFormatChanging = false;
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        await ReportProgressAsync(Player.Position);
        ClearView();
        Danmaku.ClearAll();
        Subtitle.ClearAll();
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
        SelectedSection.TryFirstLoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task ReportProgressAsync(int progress)
    {
        bool shouldReport = _settings.ShouldReportProgress;
        if (
            !shouldReport
            || _view is null
            || _episode is null
            || _view.Information is null
            || progress == 0
            || Player.IsPlayerDataLoading
        )
        {
            return;
        }

        try
        {
            CancellationTokenSource cancellationToken = new(TimeSpan.FromSeconds(2));
            long aid = _episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Aid);
            long cid = _episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Cid);
            await _service.ReportEpisodeProgressAsync(
                aid.ToString(),
                cid.ToString(),
                _episode.Identifier.Id,
                _view.Information.Identifier.Id,
                progress,
                cancellationToken.Token
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试上报播放进度时失败.");
        }
    }

    partial void OnPlayerWidthChanged(double value) => CalcPlayerHeight();
}
