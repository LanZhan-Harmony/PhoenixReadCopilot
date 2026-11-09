using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Moment;
using Richasy.BiliKernel.Bili.Search;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.User;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 用户动态页视图模型.
/// </summary>
public sealed partial class UserSpacePageViewModel
{
    private readonly IRelationshipService _relationshipService;
    private readonly IUserService _userService;
    private readonly ISearchService _searchService;
    private readonly ILogger<UserSpacePageViewModel> _logger;
    private readonly AppViewModel _appViewModel;
    private readonly PinnerViewModel _pinnerViewModel;
    private readonly AppSettings _settings;
    private readonly IMomentDiscoveryService _momentDiscoveryService;
    private readonly ILoggerFactory _loggerFactory;
    private UserProfile _profile;
    private int _searchPn;
    private bool _preventLoadMoreSearch;
    private CancellationTokenSource _searchCancellationTokenSource;

    [ObservableProperty]
    public partial List<UserMomentDetailViewModel> Sections { get; set; }

    [ObservableProperty]
    public partial UserMomentDetailViewModel SelectedSection { get; set; }

    [ObservableProperty]
    public partial bool IsCommentsOpened { get; set; }

    [ObservableProperty]
    public partial string UserName { get; set; }

    [ObservableProperty]
    public partial bool IsFollowed { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial bool IsSearchMode { get; set; }

    [ObservableProperty]
    public partial string SearchKeyword { get; set; }

    [ObservableProperty]
    public partial bool IsSearchEmpty { get; set; }

    [ObservableProperty]
    public partial UserCard Card { get; set; }

    /// <summary>
    /// 区块初始化完成.
    /// </summary>
    public event EventHandler Initialized;

    /// <summary>
    /// 搜索更新.
    /// </summary>
    public event EventHandler SearchUpdated;

    /// <summary>
    /// 评论模块.
    /// </summary>
    public CommentMainViewModel CommentModule { get; }

    /// <summary>
    /// 搜索视频结果.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> SearchVideos { get; } = [];
}
