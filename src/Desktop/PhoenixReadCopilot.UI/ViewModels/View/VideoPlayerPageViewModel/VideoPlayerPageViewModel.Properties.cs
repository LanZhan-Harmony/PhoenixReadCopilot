using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.Items;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 短剧播放器页面视图模型.
/// </summary>
public sealed partial class VideoPlayerPageViewModel
{
    private readonly NavigationViewModel _navigationViewModel;
    private readonly PinnerViewModel _pinnerViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<VideoPlayerPageViewModel> _logger;
    private readonly CommentMainViewModel _comments;
    private readonly PhoenixReadApiClient _apiClient;
    private readonly DramaReviewSectionDetailViewModel _dramaReviews;

    private DramaDetails? _dramaDetails;
    private string _seriesId = string.Empty;
    private string _currentVideoId = string.Empty;
    private IReadOnlyList<PlaybackMedia>? _currentMediaList;
    private IReadOnlyList<Episode>? _episodes;
    private Episode? _currentEpisode;
    private IList<Drama>? _dramaPlaylist;

    private CancellationTokenSource? _pageLoadCancellationTokenSource;
    private CancellationTokenSource? _dashLoadCancellationTokenSource;

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
    public partial bool IsPrivatePlay { get; set; }

    [ObservableProperty]
    public partial bool IsInteractionVideo { get; set; }

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
    /// 下载视图模型.
    /// </summary>
    public DownloadViewModel Downloader { get; }
}
