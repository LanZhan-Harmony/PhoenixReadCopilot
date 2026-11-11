using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 流行与榜单页面视图模型.
/// </summary>
public sealed partial class PopularPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PopularPageViewModel"/> class.
    /// </summary>
    public PopularPageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<PopularPageViewModel> logger,
        NavigationViewModel navigationViewModel,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _apiClient = apiClient;
        _logger = logger;
        _navigationViewModel = navigationViewModel;
        _dispatcherQueue = dispatcherQueue;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(PopularPage);

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Sections.Count > 0)
        {
            return;
        }

        // 1. 推荐流
        Sections.Add(
            new PopularSectionItemViewModel(
                FluentIcons.Common.Symbol.Balloon,
                Resources.Recommend,
                PopularSectionType.Recommend
            )
        );

        // 2. 推荐榜
        Sections.Add(
            new PopularSectionItemViewModel(
                FluentIcons.Common.Symbol.RibbonStar,
                Resources.BoardRecommend,
                PopularSectionType.LeaderboardRecommend,
                leaderboardType: ShortDramaLeaderboardType.Recommend
            )
        );

        // 3. 热播榜
        Sections.Add(
            new PopularSectionItemViewModel(
                FluentIcons.Common.Symbol.Fire,
                Resources.BoardHotPlay,
                PopularSectionType.LeaderboardHotPlay,
                leaderboardType: ShortDramaLeaderboardType.HotPlay
            )
        );

        // 4. 新剧榜
        Sections.Add(
            new PopularSectionItemViewModel(
                FluentIcons.Common.Symbol.Sparkle,
                Resources.BoardNewRelease,
                PopularSectionType.LeaderboardNewRelease,
                leaderboardType: ShortDramaLeaderboardType.NewRelease
            )
        );

        // 5. 热搜榜
        Sections.Add(
            new PopularSectionItemViewModel(
                FluentIcons.Common.Symbol.Search,
                Resources.BoardHotSearch,
                PopularSectionType.LeaderboardHotSearch,
                leaderboardType: ShortDramaLeaderboardType.HotSearch
            )
        );

        // 6. 演员排行榜
        Sections.Add(
            new PopularSectionItemViewModel(
                FluentIcons.Common.Symbol.PeopleStar,
                Resources.LeaderboardActor,
                PopularSectionType.LeaderboardActor,
                isActorLeaderboard: true
            )
        );

        string lastSelectedSectionId = _settings.PopularPageLastSelectedSectionId;
        IPopularSectionItemViewModel? targetSection = null;
        if (
            !string.IsNullOrEmpty(lastSelectedSectionId)
            && Enum.TryParse<PopularSectionType>(
                lastSelectedSectionId,
                out PopularSectionType sectionType
            )
        )
        {
            targetSection = Sections
                .OfType<PopularSectionItemViewModel>()
                .FirstOrDefault(p => p.Type == sectionType);
        }

        SelectSectionCommand.Execute(targetSection ?? Sections.First());
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task SelectSectionAsync(IPopularSectionItemViewModel vm)
    {
        if (vm is null || vm == SelectedSection)
        {
            return;
        }

        // 取消正在进行的请求
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        IsVideoLoading = false;

        // 立即清空当前显示列表，防止界面残留或不同榜单互相串数据
        Videos.Clear();

        SelectedSection = vm;
        if (vm is PopularSectionItemViewModel pSection)
        {
            _settings.PopularPageLastSelectedSectionId = pSection.Type.ToString();
            IsLeaderboardSection = pSection.LeaderboardType.HasValue;
        }
        else
        {
            IsLeaderboardSection = false;
        }

        string cacheKey = GetCacheKey(vm, CurrentChannel);
        if (
            _videoCache.TryGetValue(cacheKey, out List<VideoItemViewModel>? cacheVideos)
            && cacheVideos.Count > 0
        )
        {
            foreach (VideoItemViewModel video in cacheVideos)
            {
                Videos.Add(video);
            }

            VideoListUpdated?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ResetPagination(vm);
            await LoadVideosAsync();
        }
    }

    [RelayCommand]
    private async Task ChangeChannelAsync(string channelName)
    {
        if (
            !Enum.TryParse<ShortDramaLeaderboardChannel>(
                channelName,
                out ShortDramaLeaderboardChannel channel
            )
        )
        {
            return;
        }

        if (CurrentChannel == channel)
        {
            return;
        }

        // 取消正在进行的请求
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        IsVideoLoading = false;

        // 立即清空当前列表
        Videos.Clear();
        CurrentChannel = channel;

        if (SelectedSection is not null)
        {
            string cacheKey = GetCacheKey(SelectedSection, CurrentChannel);
            if (
                _videoCache.TryGetValue(cacheKey, out List<VideoItemViewModel>? cacheVideos)
                && cacheVideos.Count > 0
            )
            {
                foreach (VideoItemViewModel video in cacheVideos)
                {
                    Videos.Add(video);
                }

                VideoListUpdated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _leaderboardOffset = 0;
                _leaderboardSessionId = null;
                _hasMoreLeaderboard = true;
                await LoadVideosAsync();
            }
        }
    }

    [RelayCommand]
    private Task ChangeBoardAsync(string boardName) => ChangeChannelAsync(boardName);

    [RelayCommand]
    private async Task LoadVideosAsync()
    {
        if (IsVideoLoading)
        {
            return;
        }

        IsVideoLoading = true;
        long requestId = ++_currentRequestId;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        CancellationToken ct = _cancellationTokenSource.Token;

        try
        {
            if (SelectedSection is PopularSectionItemViewModel section)
            {
                if (section.Type == PopularSectionType.Recommend)
                {
                    await LoadRecommendVideosAsync(requestId, ct);
                }
                else if (section.LeaderboardType.HasValue)
                {
                    await LoadDramaLeaderboardAsync(
                        section.LeaderboardType.Value,
                        CurrentChannel,
                        requestId,
                        ct
                    );
                }
                else if (section.IsActorLeaderboard)
                {
                    await LoadActorLeaderboardAsync(requestId, ct);
                }
            }
        }
        finally
        {
            if (requestId == _currentRequestId)
            {
                VideoListUpdated?.Invoke(this, EventArgs.Empty);
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    IsVideoLoading = false
                );
            }
        }
    }

    [RelayCommand]
    private async Task RefreshVideoAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        IsVideoLoading = false;

        Videos.Clear();
        if (SelectedSection is not null)
        {
            _videoCache.Remove(GetCacheKey(SelectedSection, CurrentChannel));
            ResetPagination(SelectedSection);
        }

        await LoadVideosAsync();
    }

    private void ResetPagination(IPopularSectionItemViewModel section)
    {
        if (section is PopularSectionItemViewModel pSection)
        {
            if (pSection.Type == PopularSectionType.Recommend)
            {
                _recommendOffset = 0;
                _recommendSessionId = null;
                _seenDramaIds.Clear();
                _hasMoreRecommendations = true;
            }
            else if (pSection.LeaderboardType.HasValue)
            {
                _leaderboardOffset = 0;
                _leaderboardSessionId = null;
                _hasMoreLeaderboard = true;
            }
            else if (pSection.IsActorLeaderboard)
            {
                _actorOffset = 0;
                _actorSessionId = null;
                _hasMoreActor = true;
            }
        }
    }

    private static string GetCacheKey(
        IPopularSectionItemViewModel section,
        ShortDramaLeaderboardChannel channel
    )
    {
        if (section is PopularSectionItemViewModel pSection)
        {
            if (pSection.IsActorLeaderboard)
            {
                return "Actor";
            }

            if (pSection.LeaderboardType.HasValue)
            {
                return $"Leaderboard_{pSection.LeaderboardType.Value}_{channel}";
            }

            return pSection.Type.ToString();
        }

        return section?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// 短剧排行榜受众频道列表.
    /// </summary>
    public static readonly IReadOnlyList<(
        string Title,
        ShortDramaLeaderboardChannel Channel
    )> Channels =
    [
        (Resources.ChannelAll, ShortDramaLeaderboardChannel.All),
        (Resources.ChannelFemale, ShortDramaLeaderboardChannel.Female),
        (Resources.ChannelMale, ShortDramaLeaderboardChannel.Male),
    ];

    [RelayCommand]
    private void PlayCuratedList()
    {
        VideoItemViewModel? first = Videos.FirstOrDefault();
        if (first?.Drama is not null)
        {
            _navigationViewModel.NavigateToOver(typeof(VideoPlayerPage), first.Drama);
        }
    }
}
