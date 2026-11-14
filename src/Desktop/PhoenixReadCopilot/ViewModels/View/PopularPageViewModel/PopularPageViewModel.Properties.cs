using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 流行与榜单页面视图模型 (属性分部).
/// </summary>
public sealed partial class PopularPageViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<PopularPageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
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
    public partial IPopularSectionItemViewModel? SelectedSection { get; set; }

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
    public ObservableCollection<IPopularSectionItemViewModel> Sections { get; } = [];

    /// <summary>
    /// 短剧视频榜单列表 (供短剧榜单 Repeater 使用).
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Videos { get; } = [];

    /// <summary>
    /// 演员榜单列表 (供演员榜单 Repeater 使用).
    /// </summary>
    public ObservableCollection<ActorItemViewModel> Actors { get; } = [];
}
