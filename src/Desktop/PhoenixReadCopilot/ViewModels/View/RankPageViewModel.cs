using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 榜单页面视图模型.
/// </summary>
public sealed partial class RankPageViewModel : LayoutPageViewModelBase
{
    private readonly IRankService _rankService;
    private readonly ILogger<RankPageViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Dictionary<string, List<VideoItemViewModel>> _videoCache = [];
    private readonly Dictionary<string, List<ActorItemViewModel>> _actorCache = [];

    private int _leaderboardOffset;
    private string? _leaderboardSessionId;
    private bool _hasMoreLeaderboard = true;

    private int _actorOffset;
    private string? _actorSessionId;
    private bool _hasMoreActor = true;

    private long _currentRequestId;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    public partial bool IsPartitionLoading { get; set; }

    [ObservableProperty]
    public partial bool IsVideoLoading { get; set; }

    [ObservableProperty]
    public partial RankSectionItemViewModel? SelectedSection { get; set; }

    [ObservableProperty]
    public partial bool IsLeaderboardSection { get; set; }

    /// <summary>
    /// 当前是否处于演员榜.
    /// </summary>
    [ObservableProperty]
    public partial bool IsActorLeaderboard { get; set; }

    [ObservableProperty]
    public partial ShortDramaLeaderboardChannel CurrentChannel { get; set; } =
        ShortDramaLeaderboardChannel.All;

    [ObservableProperty]
    public partial string? BoardTitle { get; set; }

    /// <summary>
    /// 区块加载完成.
    /// </summary>
    public event EventHandler? SectionInitialized;

    /// <summary>
    /// 视频/榜单列表已完成更新.
    /// </summary>
    public event EventHandler? VideoListUpdated;

    /// <summary>
    /// 分区与榜单列表.
    /// </summary>
    public ObservableCollection<RankSectionItemViewModel> Sections { get; } = [];

    /// <summary>
    /// 短剧视频榜单列表 (供短剧榜单 Repeater 使用).
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Videos { get; } = [];

    /// <summary>
    /// 演员榜单列表 (供演员榜单 Repeater 使用).
    /// </summary>
    public ObservableCollection<ActorItemViewModel> Actors { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="RankPageViewModel"/> class.
    /// </summary>
    public RankPageViewModel(
        IRankService rankService,
        ILogger<RankPageViewModel> logger,
        INavigationService navigationService,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _rankService = rankService;
        _logger = logger;
        _navigationService = navigationService;
        _dispatcherQueue = dispatcherQueue;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(RankPage);

    public async Task InitializeAsync()
    {
        if (Sections.Count > 0)
        {
            return;
        }

        // 1. 推荐榜
        Sections.Add(
            new RankSectionItemViewModel(
                FluentIcons.Common.Symbol.RibbonStar,
                Resources.BoardRecommend,
                RankSectionType.LeaderboardRecommend,
                leaderboardType: ShortDramaLeaderboardType.Recommend,
                pageViewModel: this
            )
        );

        // 2. 热播榜
        Sections.Add(
            new RankSectionItemViewModel(
                FluentIcons.Common.Symbol.Fire,
                Resources.BoardHotPlay,
                RankSectionType.LeaderboardHotPlay,
                leaderboardType: ShortDramaLeaderboardType.HotPlay,
                pageViewModel: this
            )
        );

        // 3. 新剧榜
        Sections.Add(
            new RankSectionItemViewModel(
                FluentIcons.Common.Symbol.Sparkle,
                Resources.BoardNewRelease,
                RankSectionType.LeaderboardNewRelease,
                leaderboardType: ShortDramaLeaderboardType.NewRelease,
                pageViewModel: this
            )
        );

        // 4. 热搜榜
        Sections.Add(
            new RankSectionItemViewModel(
                FluentIcons.Common.Symbol.Search,
                Resources.BoardHotSearch,
                RankSectionType.LeaderboardHotSearch,
                leaderboardType: ShortDramaLeaderboardType.HotSearch,
                pageViewModel: this
            )
        );

        // 5. 演员排行榜
        Sections.Add(
            new RankSectionItemViewModel(
                FluentIcons.Common.Symbol.PeopleStar,
                Resources.LeaderboardActor,
                RankSectionType.LeaderboardActor,
                isActorLeaderboard: true,
                pageViewModel: this
            )
        );

        string lastSelectedSectionId = _settings.RankPageLastSelectedSectionId;
        RankSectionItemViewModel? targetSection = null;
        if (
            !string.IsNullOrEmpty(lastSelectedSectionId)
            && Enum.TryParse<RankSectionType>(
                lastSelectedSectionId,
                out RankSectionType sectionType
            )
        )
        {
            targetSection = Sections.FirstOrDefault(p => p.Type == sectionType);
        }

        _ = SelectSectionAsync(targetSection ?? Sections.First());
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    public async Task SelectSectionAsync(RankSectionItemViewModel? vm)
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
        Actors.Clear();

        SelectedSection = vm;
        _settings.RankPageLastSelectedSectionId = vm.Type.ToString();
        IsLeaderboardSection = vm.LeaderboardType.HasValue;
        IsActorLeaderboard = vm.IsActorLeaderboard;

        string cacheKey = GetCacheKey(vm, CurrentChannel);
        if (IsActorLeaderboard)
        {
            if (
                _actorCache.TryGetValue(cacheKey, out List<ActorItemViewModel>? cacheActors)
                && cacheActors.Count > 0
            )
            {
                foreach (ActorItemViewModel actor in cacheActors)
                {
                    Actors.Add(actor);
                }

                VideoListUpdated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                ResetPagination(vm);
                await LoadVideosAsync();
            }
        }
        else
        {
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
    }

    public async Task ChangeChannelAsync(string channelName)
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

    public async Task LoadVideosAsync()
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
            if (SelectedSection is RankSectionItemViewModel section)
            {
                if (section.LeaderboardType.HasValue)
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
        Actors.Clear();
        if (SelectedSection is not null)
        {
            string cacheKey = GetCacheKey(SelectedSection, CurrentChannel);
            _videoCache.Remove(cacheKey);
            _actorCache.Remove(cacheKey);
            ResetPagination(SelectedSection);
        }

        await LoadVideosAsync();
    }

    private void ResetPagination(RankSectionItemViewModel section)
    {
        if (section.LeaderboardType.HasValue)
        {
            _leaderboardOffset = 0;
            _leaderboardSessionId = null;
            _hasMoreLeaderboard = true;
        }
        else if (section.IsActorLeaderboard)
        {
            _actorOffset = 0;
            _actorSessionId = null;
            _hasMoreActor = true;
        }
    }

    private static string GetCacheKey(
        RankSectionItemViewModel section,
        ShortDramaLeaderboardChannel channel
    )
    {
        if (section.IsActorLeaderboard)
        {
            return "Actor";
        }

        if (section.LeaderboardType.HasValue)
        {
            return $"Leaderboard_{section.LeaderboardType.Value}_{channel}";
        }

        return section.Type.ToString();
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
            _navigationService.NavigateToOver(typeof(VideoPlayerPage), first.Drama);
        }
    }

    private async Task LoadDramaLeaderboardAsync(
        ShortDramaLeaderboardType type,
        ShortDramaLeaderboardChannel channel,
        long requestId,
        CancellationToken ct
    )
    {
        if (!_hasMoreLeaderboard && _leaderboardOffset > 0)
        {
            return;
        }

        try
        {
            ShortDramaLeaderboardPage page = await _rankService.GetShortDramaLeaderboardAsync(
                type: type,
                channel: channel,
                offset: _leaderboardOffset,
                sessionId: _leaderboardSessionId,
                cancellationToken: ct
            );

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            _leaderboardOffset = page.NextOffset;
            _leaderboardSessionId = page.SessionId;
            _hasMoreLeaderboard = page.HasMore;

            BoardTitle = page.BoardTitle;
            if (page.Items is { Count: > 0 })
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (requestId != _currentRequestId || ct.IsCancellationRequested)
                    {
                        return;
                    }

                    int addedCount = 0;
                    foreach (Drama item in page.Items)
                    {
                        if (
                            string.IsNullOrWhiteSpace(item.Id)
                            || Videos.Any(v => v.Drama?.Id == item.Id)
                        )
                        {
                            continue;
                        }

                        VideoItemViewModel vm = new(item, VideoCardStyle.Rank)
                        {
                            Rank = Videos.Count + 1,
                        };
                        Videos.Add(vm);
                        addedCount++;
                    }

                    if (SelectedSection is not null)
                    {
                        _videoCache[GetCacheKey(SelectedSection, CurrentChannel)] = [.. Videos];
                    }

                    if (addedCount > 0)
                    {
                        VideoListUpdated?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"尝试加载红果短剧排行榜时出错. Type: {type}, Channel: {channel}"
            );
        }
    }

    private async Task LoadActorLeaderboardAsync(long requestId, CancellationToken ct)
    {
        if (!_hasMoreActor && _actorOffset > 0)
        {
            return;
        }

        try
        {
            ActorLeaderboardPage page = await _rankService.GetActorLeaderboardAsync(
                offset: _actorOffset,
                cancellationToken: ct
            );

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            _actorOffset = page.NextOffset;
            _actorSessionId = page.SessionId;
            _hasMoreActor = page.HasMore;

            if (page.Items is { Count: > 0 })
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (requestId != _currentRequestId || ct.IsCancellationRequested)
                    {
                        return;
                    }

                    int addedCount = 0;
                    foreach (ActorLeaderboardItem item in page.Items)
                    {
                        string actorKey = !string.IsNullOrEmpty(item.ActorId)
                            ? item.ActorId
                            : item.UserId;
                        if (
                            string.IsNullOrWhiteSpace(actorKey)
                            || Actors.Any(v => v.ActorId == actorKey)
                        )
                        {
                            continue;
                        }

                        ActorItemViewModel vm = new(item)
                        {
                            Rank = item.Position > 0 ? item.Position : Actors.Count + 1,
                        };
                        Actors.Add(vm);
                        addedCount++;
                    }

                    if (SelectedSection is not null)
                    {
                        _actorCache[GetCacheKey(SelectedSection, CurrentChannel)] = [.. Actors];
                    }

                    if (addedCount > 0)
                    {
                        VideoListUpdated?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载红果演员排行榜时出错.");
        }
    }
}
