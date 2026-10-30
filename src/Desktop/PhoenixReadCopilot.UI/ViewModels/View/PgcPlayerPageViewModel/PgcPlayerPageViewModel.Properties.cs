using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Media;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// PGC 播放器页面视图模型.
/// </summary>
public sealed partial class PgcPlayerPageViewModel
{
    private readonly IPlayerService _service;
    private readonly IEntertainmentDiscoveryService _discoveryService;
    private readonly IFavoriteService _favoriteService;
    private readonly ILogger<PgcPlayerPageViewModel> _logger;
    private readonly CommentMainViewModel _comments;

    private CancellationTokenSource _pageLoadCancellationTokenSource;
    private CancellationTokenSource _dashLoadCancellationTokenSource;

    private PgcPlayerView? _view;
    private EpisodeInformation? _episode;
    private IList<DashSegmentInformation>? _videoSegments;
    private IList<DashSegmentInformation>? _audioSegments;
    private int _initialProgress;
    private int? _injectedProgress;
    private EntertainmentType _type;
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
    public partial string SeasonTitle { get; set; }

    [ObservableProperty]
    public partial string EpisodeTitle { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; }

    [ObservableProperty]
    public partial string Alias { get; set; }

    [ObservableProperty]
    public partial bool IsFollow { get; set; }

    [ObservableProperty]
    public partial double PlayerWidth { get; set; }

    [ObservableProperty]
    public partial double PlayerHeight { get; set; }

    [ObservableProperty]
    public partial double PlayCount { get; set; }

    [ObservableProperty]
    public partial double DanmakuCount { get; set; }

    [ObservableProperty]
    public partial double CommentCount { get; set; }

    [ObservableProperty]
    public partial double LikeCount { get; set; }

    [ObservableProperty]
    public partial double CoinCount { get; set; }

    [ObservableProperty]
    public partial double FavoriteCount { get; set; }

    [ObservableProperty]
    public partial double FollowCount { get; set; }

    [ObservableProperty]
    public partial string OnlineCountText { get; set; }

    [ObservableProperty]
    public partial string SeasonId { get; set; }

    [ObservableProperty]
    public partial string EpisodeId { get; set; }

    [ObservableProperty]
    public partial bool IsLiked { get; set; }

    [ObservableProperty]
    public partial bool IsCoined { get; set; }

    [ObservableProperty]
    public partial bool IsFavorited { get; set; }

    [ObservableProperty]
    public partial bool IsCoinAlsoLike { get; set; }

    [ObservableProperty]
    public partial bool HasNextEpisode { get; set; }

    [ObservableProperty]
    public partial string NextEpisodeTip { get; set; }

    [ObservableProperty]
    public partial PlayerFormatItemViewModel? SelectedFormat { get; set; }

    [ObservableProperty]
    public partial IPlayerSectionDetailViewModel? SelectedSection { get; set; }

    [ObservableProperty]
    public partial List<PlayerFormatItemViewModel>? Formats { get; set; }

    [ObservableProperty]
    public partial List<PlayerFavoriteFolderViewModel>? FavoriteFolders { get; set; }

    [ObservableProperty]
    public partial List<IPlayerSectionDetailViewModel>? Sections { get; set; }

    /// <summary>
    /// 视图数据加载完成.
    /// </summary>
    public event EventHandler ViewInitialized;

    /// <summary>
    /// 区块加载完成.
    /// </summary>
    public event EventHandler SectionInitialized;

    /// <summary>
    /// 弹幕视图模型.
    /// </summary>
    public DanmakuViewModel Danmaku { get; }

    /// <summary>
    /// 字幕视图模型.
    /// </summary>
    public SubtitleViewModel Subtitle { get; }

    /// <summary>
    /// 下载视图模型.
    /// </summary>
    public DownloadViewModel Downloader { get; }
}
