using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Controls.Components;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 用户空间与个人主页视图模型（支持个人主页与他人空间共用）.
/// </summary>
internal sealed partial class UserSpacePageViewModel : LayoutPageViewModelBase
{
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authService;
    private readonly IFollowService _followService;
    private readonly ILikeService _likeService;
    private readonly IHistoryService _historyService;
    private readonly IFavoriteService _favoriteService;
    private readonly IViewLaterService _viewLaterService;
    private readonly IReservationService _reservationService;
    private readonly INavigationService _navigationService;
    private readonly AppViewModel _appViewModel;
    private readonly PinnerViewModel _pinnerViewModel;
    private readonly ILogger<UserSpacePageViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    private CancellationTokenSource? _loadingCts;
    private long _currentRequestId;
    private string _userId = string.Empty;
    private UserProfile? _phoenixProfile;
    private string _worksCursor = string.Empty;
    private string _circleCursor = string.Empty;
    private int _offset;

    private readonly UserSpaceSectionItemViewModel _followsSection;
    private readonly UserSpaceSectionItemViewModel _fansSection;

    public event EventHandler? Initialized;
    public event EventHandler? SectionInitialized;
    public event EventHandler? DataUpdated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyHolder))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasMore { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyHolder))]
    public partial bool IsEmpty { get; set; }

    public bool ShowEmptyHolder => !IsLoading && IsEmpty;

    [ObservableProperty]
    public partial string EmptyDescription { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenFollows))]
    [NotifyPropertyChangedFor(nameof(CanOpenFans))]
    public partial bool IsMe { get; set; }

    [ObservableProperty]
    public partial bool IsAnonymous { get; set; }

    [ObservableProperty]
    public partial string UserName { get; set; }

    [ObservableProperty]
    public partial string HongguoId { get; set; }

    [ObservableProperty]
    public partial string AvatarUrl { get; set; }

    [ObservableProperty]
    public partial string BackgroundCoverUrl { get; set; }

    [ObservableProperty]
    public partial string Introduce { get; set; }

    [ObservableProperty]
    public partial bool IsVerified { get; set; }

    [ObservableProperty]
    public partial bool IsCreator { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FansCountText))]
    [NotifyPropertyChangedFor(nameof(CanOpenFans))]
    public partial long FansCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FollowCountText))]
    [NotifyPropertyChangedFor(nameof(CanOpenFollows))]
    public partial long FollowCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LikeCountText))]
    public partial long LikeCount { get; set; }

    public string FansCountText => FormatHelper.FormatCount(FansCount);
    public string FollowCountText => FormatHelper.FormatCount(FollowCount);
    public string LikeCountText => FormatHelper.FormatCount(LikeCount);

    public bool CanOpenFollows =>
        (IsMe && FollowCount > 0)
        || (!string.IsNullOrEmpty(_userId) && FollowCount > 0 && IsFollowsAccessible);
    public bool CanOpenFans =>
        (IsMe && FansCount > 0)
        || (!string.IsNullOrEmpty(_userId) && FansCount > 0 && IsFansAccessible);

    [ObservableProperty]
    public partial bool CanFollow { get; set; }

    [ObservableProperty]
    public partial bool IsFollowed { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenFans))]
    public partial bool IsFansAccessible { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanOpenFollows))]
    public partial bool IsFollowsAccessible { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> Badges { get; set; }

    [ObservableProperty]
    public partial UserCard? Card { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UserSpaceSectionItemViewModel> Sections { get; set; }

    [ObservableProperty]
    public partial UserSpaceSectionItemViewModel? SelectedSection { get; set; }

    /// <summary>
    /// 当前活动主体内容条目（驱动 ContentControl 模板选择器自动感知 Section/SubTab 切换）.
    /// </summary>
    [ObservableProperty]
    public partial IUserSpaceContentItem? CurrentContent { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<VideoItemViewModel> Videos { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UserReviewItemViewModel> Reviews { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<CircleFeedItemViewModel> CircleFeeds { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<UserItemViewModel> Relations { get; set; }

    public UserSpacePageViewModel(
        IUserService userService,
        IAuthenticationService authService,
        IFollowService followService,
        ILikeService likeService,
        IHistoryService historyService,
        IFavoriteService favoriteService,
        IViewLaterService viewLaterService,
        IReservationService reservationService,
        INavigationService navigationService,
        ISettingsService settingsService,
        AppViewModel appViewModel,
        PinnerViewModel pinnerViewModel,
        ILogger<UserSpacePageViewModel> logger,
        DispatcherQueue dispatcherQueue
    )
        : base(settingsService)
    {
        _userService = userService;
        _authService = authService;
        _followService = followService;
        _likeService = likeService;
        _historyService = historyService;
        _favoriteService = favoriteService;
        _viewLaterService = viewLaterService;
        _reservationService = reservationService;
        _navigationService = navigationService;
        _appViewModel = appViewModel;
        _pinnerViewModel = pinnerViewModel;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;

        _followsSection = new(
            "Follows",
            Resources.Follow,
            Symbol.PeopleCommunity,
            pageViewModel: this
        );
        _fansSection = new("Fans", Resources.Fans, Symbol.HeartPulse, pageViewModel: this);

        Sections = [];
        Videos = [];
        Reviews = [];
        CircleFeeds = [];
        Relations = [];
        Badges = [];

        SetupCollectionWatchers();

        _followService.FollowChanged += OnFollowServiceChanged;
        _likeService.LikeChanged += OnLikeServiceChanged;

        EmptyDescription = Resources.NoSpecificData;
        UserName = string.Empty;
        HongguoId = string.Empty;
        AvatarUrl = string.Empty;
        BackgroundCoverUrl = string.Empty;
        Introduce = string.Empty;

        NavColumnWidth = 260;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => "UserSpacePage";

    public async Task InitializeAsync(object? parameter)
    {
        long requestId = ++_currentRequestId;
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();
        CancellationToken ct = _loadingCts.Token;

        IsLoading = true;
        try
        {
            UserProfile? profile = null;
            string fallbackAvatar = string.Empty;
            string fallbackName = string.Empty;
            string targetUserId = string.Empty;
            string? targetSection = null;

            if (parameter is null)
            {
                IsMe = true;
                if (_authService.IsAuthenticated && _authService.CurrentUser is { } myUser)
                {
                    targetUserId = myUser.Id;
                    fallbackName = myUser.Name;
                    fallbackAvatar = myUser.AvatarUrl;
                }
            }
            else if (parameter is UserSpaceNavArgs navArgs)
            {
                targetUserId = navArgs.UserId ?? string.Empty;
                targetSection = navArgs.TargetSection;
                IsMe =
                    string.IsNullOrEmpty(targetUserId)
                    || (
                        _authService.IsAuthenticated && _authService.CurrentUser?.Id == targetUserId
                    );
                if (IsMe && _authService.IsAuthenticated && _authService.CurrentUser is { } myUser)
                {
                    targetUserId = myUser.Id;
                    fallbackName = myUser.Name;
                    fallbackAvatar = myUser.AvatarUrl;
                }
            }
            else if (parameter is string str)
            {
                if (
                    str
                    is "History"
                        or "Favorite"
                        or "ViewLater"
                        or "Reservation"
                        or "Works"
                        or "Likes"
                        or "Reviews"
                        or "Circle"
                        or "Follows"
                        or "Fans"
                )
                {
                    IsMe = true;
                    targetSection = str;
                    if (_authService.IsAuthenticated && _authService.CurrentUser is { } myUser)
                    {
                        targetUserId = myUser.Id;
                        fallbackName = myUser.Name;
                        fallbackAvatar = myUser.AvatarUrl;
                    }
                }
                else
                {
                    targetUserId = str;
                    IsMe = _authService.IsAuthenticated && _authService.CurrentUser?.Id == str;
                }
            }
            else if (parameter is DramaCredit credit)
            {
                targetUserId = credit.User?.Id ?? string.Empty;
                fallbackName = credit.User?.Name ?? string.Empty;
                fallbackAvatar = credit.User?.AvatarUrl ?? string.Empty;
                IsMe = _authService.IsAuthenticated && _authService.CurrentUser?.Id == targetUserId;
            }
            else if (parameter is ActorLeaderboardItem actor)
            {
                targetUserId = !string.IsNullOrEmpty(actor.UserId) ? actor.UserId : actor.ActorId;
                fallbackName = actor.Name;
                fallbackAvatar = actor.AvatarUrl ?? string.Empty;
                IsMe = _authService.IsAuthenticated && _authService.CurrentUser?.Id == targetUserId;
            }
            else if (parameter is UserProfile up)
            {
                targetUserId = up.User.Id;
                profile = up;
                IsMe = _authService.IsAuthenticated && _authService.CurrentUser?.Id == targetUserId;
            }
            else if (parameter is PhoenixReadApi.User apiUser)
            {
                targetUserId = apiUser.Id;
                fallbackName = apiUser.Name;
                fallbackAvatar = apiUser.AvatarUrl ?? string.Empty;
                IsMe = _authService.IsAuthenticated && _authService.CurrentUser?.Id == targetUserId;
            }

            _userId = targetUserId;

            if (!string.IsNullOrEmpty(targetUserId))
            {
                try
                {
                    profile ??= await _userService.GetUserProfileAsync(targetUserId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取网络用户资料失败: {UserId}", targetUserId);
                }
            }

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            _phoenixProfile = profile;
            ApplyProfile(profile, fallbackName, fallbackAvatar);

            if (IsMe && !_authService.IsAuthenticated)
            {
                FollowCount = await _followService.GetLocalFollowingsCountAsync(ct);
                IsFollowsAccessible = FollowCount > 0;
            }

            BuildSections(profile);

            // 导航到指定 Section 或默认第一个
            UserSpaceSectionItemViewModel? initialSection = null;
            if (!string.IsNullOrEmpty(targetSection))
            {
                if (targetSection.Equals("Follows", StringComparison.OrdinalIgnoreCase))
                {
                    initialSection = _followsSection;
                }
                else if (targetSection.Equals("Fans", StringComparison.OrdinalIgnoreCase))
                {
                    initialSection = _fansSection;
                }
                else
                {
                    initialSection = Sections.FirstOrDefault(s =>
                        s.Id.Equals(targetSection, StringComparison.OrdinalIgnoreCase)
                    ) ?? Sections.FirstOrDefault();
                }
            }
            else
            {
                initialSection = Sections.FirstOrDefault();
            }

            SectionInitialized?.Invoke(this, EventArgs.Empty);
            Initialized?.Invoke(this, EventArgs.Empty);

            if (initialSection != null)
            {
                await SelectSectionAsync(initialSection, force: true);
            }
            else
            {
                SelectedSection = null;
                IsEmpty = true;
                EmptyDescription = Resources.NoSpecificData;
                IsLoading = false;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化用户空间失败");
            IsLoading = false;
        }
    }

    private void SetupCollectionWatchers()
    {
        Videos.CollectionChanged += (_, _) => OnAnyCollectionChanged();
        Reviews.CollectionChanged += (_, _) => OnAnyCollectionChanged();
        CircleFeeds.CollectionChanged += (_, _) => OnAnyCollectionChanged();
        Relations.CollectionChanged += (_, _) => OnAnyCollectionChanged();
    }

    private void OnAnyCollectionChanged()
    {
        if (SelectedSection != null)
        {
            bool hasContent = SelectedSection.Id switch
            {
                "Works" => Videos.Count > 0 || Reviews.Count > 0,
                "Circle" => CircleFeeds.Count > 0,
                "Reviews" => Reviews.Count > 0,
                "Follows" or "Fans" => Relations.Count > 0,
                _ => Videos.Count > 0,
            };

            if (hasContent)
            {
                IsEmpty = false;
            }
        }
    }

    private void ApplyProfile(UserProfile? profile, string fallbackName, string fallbackAvatar)
    {
        if (profile is not null)
        {
            UserName = !string.IsNullOrWhiteSpace(profile.User.Name)
                ? profile.User.Name
                : fallbackName;
            HongguoId = !string.IsNullOrWhiteSpace(profile.HongguoId)
                ? profile.HongguoId
                : profile.User.Id;
            AvatarUrl = !string.IsNullOrWhiteSpace(profile.User.AvatarUrl)
                ? profile.User.AvatarUrl
                : fallbackAvatar;
            BackgroundCoverUrl = profile.BackgroundCoverUrl;
            Introduce = profile.User.Introduction;
            IsVerified = profile.IsVerified;
            IsCreator = profile.IsCreator;
            FansCount = profile.FansCount;
            FollowCount = profile.FollowCount;
            LikeCount = profile.LikeCount;
            CanFollow = !IsMe && profile.CanFollow;
            IsFollowed = !IsMe && (profile.IsFollowing || _followService.IsFollowing(_userId));

            IsFansAccessible = profile.FansCount > 0;
            IsFollowsAccessible = profile.FollowCount > 0;

            Badges.Clear();
            foreach (string role in profile.CreatorRoles)
            {
                Badges.Add(role);
            }
            foreach (string badge in profile.Badges)
            {
                Badges.Add(badge);
            }
            foreach (UserHonor honor in profile.Honors)
            {
                Badges.Add(honor.Name);
            }

            Card = CreateUserCard(profile, UserName, AvatarUrl);
        }
        else
        {
            // 本地未登录或匿名占位状态
            if (IsMe)
            {
                UserName = _authService.IsAuthenticated
                    ? (_authService.CurrentUser?.Name ?? Resources.User)
                    : "未登录用户";
                HongguoId = _authService.IsAuthenticated
                    ? (_authService.CurrentUser?.Id ?? string.Empty)
                    : "点击登录可同步云端数据";
                AvatarUrl = _authService.IsAuthenticated
                    ? (_authService.CurrentUser?.AvatarUrl ?? string.Empty)
                    : string.Empty;
                BackgroundCoverUrl = string.Empty;
                Introduce = _authService.IsAuthenticated
                    ? string.Empty
                    : "登录后可同步播放历史、短剧收藏、追剧预约与个人评论";
            }
            else
            {
                UserName = !string.IsNullOrEmpty(fallbackName) ? fallbackName : Resources.User;
                HongguoId = _userId;
                AvatarUrl = fallbackAvatar;
                BackgroundCoverUrl = string.Empty;
                Introduce = string.Empty;
            }

            IsVerified = false;
            IsCreator = false;
            FansCount = 0;
            FollowCount = 0;
            LikeCount = 0;
            CanFollow = false;
            IsFollowed = false;
            IsFansAccessible = false;
            IsFollowsAccessible = false;
            Badges.Clear();
            Card = null;
        }

        IsAnonymous = IsMe && !_authService.IsAuthenticated;
    }

    private void BuildSections(UserProfile? profile)
    {
        Sections.Clear();

        if (IsMe)
        {
            // 个人主页模式
            // 1. 播放历史（未登录支持本地持久化）
            Sections.Add(
                new UserSpaceSectionItemViewModel("History", Resources.History, Symbol.History)
            );

            // 2. 我的收藏（未登录支持本地持久化）
            Sections.Add(
                new UserSpaceSectionItemViewModel(
                    "Favorite",
                    Resources.Favorite,
                    Symbol.StarLineHorizontal3
                )
            );

            // 3. 稍后再看（未登录支持本地持久化）
            Sections.Add(
                new UserSpaceSectionItemViewModel(
                    "ViewLater",
                    Resources.ViewLater,
                    Symbol.CalendarClock
                )
            );

            // 4. 作品（登录后可用）
            if (_authService.IsAuthenticated)
            {
                UserSpaceSectionItemViewModel worksSection = new(
                    "Works",
                    Resources.UserWorks,
                    Symbol.VideoClip
                );
                worksSection.SubTabs.Add(
                    new UserSpaceSubTabItemViewModel("Drama", Resources.Drama, UserWorkSubTab.Drama)
                );
                worksSection.SubTabs.Add(
                    new UserSpaceSubTabItemViewModel(
                        "Collection",
                        Resources.UserCollections,
                        UserWorkSubTab.Collection
                    )
                );
                worksSection.SelectedSubTab = worksSection.SubTabs[0];
                Sections.Add(worksSection);
            }

            // 5. 剧集预约：待上线、已上线
            UserSpaceSectionItemViewModel reservationSection = new(
                "Reservation",
                Resources.Reserve,
                Symbol.Calendar
            );
            reservationSection.SubTabs.Add(
                new UserSpaceSubTabItemViewModel("Upcoming", Resources.Upcoming, false)
            );
            reservationSection.SubTabs.Add(
                new UserSpaceSubTabItemViewModel("Online", Resources.Online, true)
            );
            reservationSection.SelectedSubTab = reservationSection.SubTabs[0];
            Sections.Add(reservationSection);

            // 6. 我的点赞（未登录支持本地离线分集存储，登录支持云端同步）
            Sections.Add(
                new UserSpaceSectionItemViewModel("Likes", Resources.UserLikes, Symbol.Heart)
            );

            // 7. 我的评论（预留接口）
            if (_authService.IsAuthenticated)
            {
                UserSpaceSectionItemViewModel reviewsSection = new(
                    "Reviews",
                    Resources.UserReviews,
                    Symbol.Comment
                );
                reviewsSection.SubTabs.Add(
                    new UserSpaceSubTabItemViewModel(
                        "Review",
                        Resources.UserReviews,
                        UserWorkSubTab.Review
                    )
                );
                reviewsSection.SelectedSubTab = reviewsSection.SubTabs[0];
                Sections.Add(reviewsSection);
            }
        }
        else
        {
            // 他人/创作者/演职人员主页模式
            if (profile != null)
            {
                foreach (UserProfileTab tab in profile.AvailableTabs)
                {
                    bool isWorks = tab.Type == UserProfileTabType.Work;
                    bool isCircle = tab.Type == UserProfileTabType.Circle;
                    bool isFavorites = tab.Type == UserProfileTabType.Favorite;
                    bool isLikes = tab.Type == UserProfileTabType.Like;

                    // 数量为 0 的 Tab 不要显示（同时若本地有内容则保留）
                    long effectiveTotal = tab.Total;
                    if (isWorks && effectiveTotal <= 0 && profile.Works.Count > 0)
                    {
                        effectiveTotal = profile.Works.Count;
                    }
                    else if (isCircle && effectiveTotal <= 0 && profile.CircleItems.Count > 0)
                    {
                        effectiveTotal = profile.CircleItems.Count;
                    }
                    else if (isFavorites && effectiveTotal <= 0 && profile.Collections.Count > 0)
                    {
                        effectiveTotal = profile.Collections.Count;
                    }
                    else if (isLikes && effectiveTotal <= 0 && profile.Likes.Count > 0)
                    {
                        effectiveTotal = profile.Likes.Count;
                    }

                    if (effectiveTotal <= 0)
                    {
                        continue;
                    }

                    if (isWorks)
                    {
                        UserSpaceSectionItemViewModel worksSection = new(
                            "Works",
                            Resources.UserWorks,
                            Symbol.VideoClip,
                            effectiveTotal
                        );
                        worksSection.SubTabs.Add(
                            new UserSpaceSubTabItemViewModel(
                                "Drama",
                                Resources.Drama,
                                UserWorkSubTab.Drama
                            )
                        );
                        worksSection.SubTabs.Add(
                            new UserSpaceSubTabItemViewModel(
                                "Reservation",
                                Resources.Reserve,
                                UserWorkSubTab.Reservation
                            )
                        );
                        worksSection.SubTabs.Add(
                            new UserSpaceSubTabItemViewModel(
                                "Collection",
                                Resources.UserCollections,
                                UserWorkSubTab.Collection
                            )
                        );
                        worksSection.SubTabs.Add(
                            new UserSpaceSubTabItemViewModel(
                                "Review",
                                Resources.UserReviews,
                                UserWorkSubTab.Review
                            )
                        );
                        worksSection.SelectedSubTab = worksSection.SubTabs[0];
                        Sections.Add(worksSection);
                    }
                    else if (isCircle)
                    {
                        UserSpaceSectionItemViewModel circleSection = new(
                            "Circle",
                            Resources.CircleFeed,
                            Symbol.DesignIdeas,
                            effectiveTotal
                        );
                        circleSection.SubTabs.Add(
                            new UserSpaceSubTabItemViewModel(
                                "Post",
                                Resources.Article,
                                CircleItemType.Post
                            )
                        );
                        circleSection.SubTabs.Add(
                            new UserSpaceSubTabItemViewModel(
                                "Video",
                                Resources.ShortVideo,
                                CircleItemType.Video
                            )
                        );
                        circleSection.SelectedSubTab = circleSection.SubTabs[0];
                        Sections.Add(circleSection);
                    }
                    else if (isFavorites)
                    {
                        Sections.Add(
                            new UserSpaceSectionItemViewModel(
                                "Favorite",
                                Resources.Favorite,
                                Symbol.StarLineHorizontal3,
                                effectiveTotal
                            )
                        );
                    }
                    else if (isLikes)
                    {
                        Sections.Add(
                            new UserSpaceSectionItemViewModel(
                                "Likes",
                                Resources.UserLikes,
                                Symbol.Heart,
                                effectiveTotal
                            )
                        );
                    }
                }
            }

            // 若无公开 Tab 兜底显示作品
            if (Sections.Count == 0 && profile is not null)
            {
                UserSpaceSectionItemViewModel worksSection = new(
                    "Works",
                    Resources.UserWorks,
                    Symbol.VideoClip
                );
                worksSection.SubTabs.Add(
                    new UserSpaceSubTabItemViewModel("Drama", Resources.Drama, UserWorkSubTab.Drama)
                );
                worksSection.SelectedSubTab = worksSection.SubTabs[0];
                Sections.Add(worksSection);
            }
        }

        foreach (UserSpaceSectionItemViewModel section in Sections)
        {
            section.PageViewModel = this;
        }
    }

    private void OnFollowServiceChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            if (IsMe && !_authService.IsAuthenticated)
            {
                FollowCount = await _followService.GetLocalFollowingsCountAsync();
                IsFollowsAccessible = FollowCount > 0;
            }
            else if (!IsMe && !string.IsNullOrEmpty(_userId))
            {
                IsFollowed = _followService.IsFollowing(_userId);
            }

            if (SelectedSection?.Id == "Follows")
            {
                await RefreshAsync();
            }
        });
    }

    private void OnLikeServiceChanged(object? sender, EventArgs e)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            if (IsMe && SelectedSection?.Id == "Likes")
            {
                await RefreshAsync();
            }
        });
    }

    /// <summary>
    /// 离开页面时注销监听与释放资源.
    /// </summary>
    public void Uninitialize()
    {
        _followService.FollowChanged -= OnFollowServiceChanged;
        _likeService.LikeChanged -= OnLikeServiceChanged;
        _loadingCts?.Cancel();
    }

    public void SelectSection(UserSpaceSectionItemViewModel? section)
    {
        _ = SelectSectionAsync(section);
    }

    public async Task SelectSectionAsync(UserSpaceSectionItemViewModel? section, bool force = false)
    {
        if (section == null)
        {
            return;
        }

        if (!force && SelectedSection == section)
        {
            return;
        }

        SelectedSection = section;
        section.PageViewModel = this;

        if (section.SelectedSubTab != null)
        {
            section.SelectedSubTab.PageViewModel = this;
            CurrentContent = section.SelectedSubTab;
        }
        else
        {
            CurrentContent = section;
        }

        await LoadSectionDataAsync(section, section.SelectedSubTab, reset: true);
    }

    public void SelectSubTab(UserSpaceSubTabItemViewModel? subTab)
    {
        _ = SelectSubTabAsync(subTab);
    }

    public async Task SelectSubTabAsync(UserSpaceSubTabItemViewModel? subTab)
    {
        if (SelectedSection == null || subTab == null || SelectedSection.SelectedSubTab == subTab)
        {
            return;
        }

        SelectedSection.SelectedSubTab = subTab;
        subTab.PageViewModel = this;
        CurrentContent = subTab;
        await LoadSectionDataAsync(SelectedSection, subTab, reset: true);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedSection != null)
        {
            await LoadSectionDataAsync(
                SelectedSection,
                SelectedSection.SelectedSubTab,
                reset: true
            );
        }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (IsLoading || !HasMore || SelectedSection == null)
        {
            return;
        }

        await LoadSectionDataAsync(SelectedSection, SelectedSection.SelectedSubTab, reset: false);
    }

    public async Task LoadSectionDataAsync(
        UserSpaceSectionItemViewModel section,
        UserSpaceSubTabItemViewModel? subTab,
        bool reset = false
    )
    {
        if (!reset && IsLoading)
        {
            return;
        }

        long requestId;
        CancellationToken ct;

        if (reset)
        {
            requestId = ++_currentRequestId;
            _loadingCts?.Cancel();
            _loadingCts = new CancellationTokenSource();
            ct = _loadingCts.Token;

            Videos.Clear();
            Reviews.Clear();
            CircleFeeds.Clear();
            Relations.Clear();

            _worksCursor = string.Empty;
            _circleCursor = string.Empty;
            _offset = 0;
            HasMore = true;
            IsEmpty = false;
        }
        else
        {
            requestId = _currentRequestId;
            ct = _loadingCts?.Token ?? CancellationToken.None;
        }

        IsLoading = true;
        try
        {
            switch (section.Id)
            {
                case "History":
                    await LoadHistoryDataAsync(requestId, ct);
                    break;
                case "Favorite":
                    await LoadFavoriteDataAsync(requestId, ct);
                    break;
                case "ViewLater":
                    await LoadViewLaterDataAsync(requestId, ct);
                    break;
                case "Reservation":
                    await LoadReservationDataAsync(subTab, requestId, ct);
                    break;
                case "Works":
                    if (subTab?.Value is UserWorkSubTab.Review)
                    {
                        await LoadReviewsDataAsync(requestId, ct);
                    }
                    else
                    {
                        await LoadWorksDataAsync(subTab, requestId, ct);
                    }
                    break;
                case "Circle":
                    await LoadCircleDataAsync(subTab, requestId, ct);
                    break;
                case "Likes":
                    await LoadLikesDataAsync(requestId, ct);
                    break;
                case "Reviews":
                    await LoadReviewsDataAsync(requestId, ct);
                    break;
                case "Follows":
                    await LoadFollowsDataAsync(requestId, ct);
                    break;
                case "Fans":
                    await LoadFansDataAsync(requestId, ct);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            if (requestId == _currentRequestId)
            {
                _logger.LogError(ex, "加载用户空间数据失败: Section={Section}", section.Id);
                if (reset)
                {
                    IsEmpty = true;
                    EmptyDescription = Resources.NoSpecificData;
                }
            }
        }
        finally
        {
            if (requestId == _currentRequestId)
            {
                IsLoading = false;

                CheckEmptyState(section);
                DataUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private async Task LoadHistoryDataAsync(long requestId, CancellationToken ct)
    {
        ReadHistoryPage historyPage = await _historyService.GetHistoryAsync(
            count: 30,
            offset: _offset,
            cancellationToken: ct
        );
        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        foreach (ReadHistoryItem item in historyPage.Items)
        {
            Videos.Add(new VideoItemViewModel(item));
        }

        _offset = historyPage.NextOffset;
        HasMore = historyPage.HasMore;
    }

    private async Task LoadFavoriteDataAsync(long requestId, CancellationToken ct)
    {
        if (IsMe)
        {
            DramaPage page = await _favoriteService.GetFavoritesAsync(
                count: 30,
                offset: _offset,
                cancellationToken: ct
            );
            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            foreach (Drama item in page.Items)
            {
                Videos.Add(new VideoItemViewModel(item));
            }

            _offset = page.NextOffset;
            HasMore = page.HasMore;
        }
        else
        {
            DramaPage page = await _userService.GetUserFavoritesAsync(
                _userId,
                count: 30,
                offset: _offset,
                cancellationToken: ct
            );
            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            foreach (Drama item in page.Items)
            {
                Videos.Add(new VideoItemViewModel(item));
            }

            _offset = page.NextOffset;
            HasMore = page.HasMore;
        }
    }

    private async Task LoadViewLaterDataAsync(long requestId, CancellationToken ct)
    {
        IReadOnlyList<Drama> items = await _viewLaterService.GetAllAsync(ViewLaterType.Drama, ct);
        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        foreach (Drama item in items)
        {
            Videos.Add(new VideoItemViewModel(item));
        }

        HasMore = false;
    }

    private async Task LoadReservationDataAsync(
        UserSpaceSubTabItemViewModel? subTab,
        long requestId,
        CancellationToken ct
    )
    {
        bool filterOnline = subTab?.Value is true;
        if (IsMe)
        {
            IReadOnlyList<ReservationItem> items = await _reservationService.GetReservationsAsync(
                count: 30,
                offset: _offset,
                cancellationToken: ct
            );
            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            foreach (ReservationItem item in items)
            {
                if (subTab != null && item.IsAvailable != filterOnline)
                {
                    continue;
                }

                Drama drama = new()
                {
                    Id = item.DramaId,
                    Title = item.Title,
                    CoverUrl = item.CoverUrl,
                };
                Videos.Add(new VideoItemViewModel(drama));
            }

            _offset += items.Count;
            HasMore = items.Count >= 30;
        }
        else
        {
            HasMore = false;
        }
    }

    private async Task LoadWorksDataAsync(
        UserSpaceSubTabItemViewModel? subTab,
        long requestId,
        CancellationToken ct
    )
    {
        UserWorkSubTab workSubTab = subTab?.Value is UserWorkSubTab st ? st : UserWorkSubTab.All;
        UserWorksQuery query = new()
        {
            SubTab = workSubTab,
            Cursor = _worksCursor,
            Count = 30,
        };

        UserWorkPage page = await _userService.GetUserWorksAsync(_userId, query, ct);
        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        foreach (UserWork item in page.Items)
        {
            if (item is Drama drama)
            {
                bool isReservationTab = workSubTab == UserWorkSubTab.Reservation;
                Videos.Add(
                    new VideoItemViewModel(drama)
                    {
                        IsOnline = !isReservationTab,
                        ReleaseNote = isReservationTab ? Resources.Upcoming : null,
                    }
                );
            }
            else if (item is UserReview review)
            {
                Reviews.Add(new UserReviewItemViewModel(review));
            }
        }

        _worksCursor = page.Cursor;
        HasMore = page.HasMore;
    }

    private async Task LoadCircleDataAsync(
        UserSpaceSubTabItemViewModel? subTab,
        long requestId,
        CancellationToken ct
    )
    {
        CircleItemType targetType = subTab?.Value is CircleItemType t ? t : CircleItemType.Post;
        CirclePage page = await _userService.GetUserCircleAsync(
            _userId,
            count: 30,
            cursor: _circleCursor,
            cancellationToken: ct
        );
        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        foreach (CircleItem item in page.Items)
        {
            if (item.Type == targetType)
            {
                CircleFeeds.Add(new CircleFeedItemViewModel(item));
            }
        }

        _circleCursor = page.Cursor;
        HasMore = page.HasMore;
    }

    private async Task LoadLikesDataAsync(long requestId, CancellationToken ct)
    {
        IReadOnlyList<Drama> dramaList;
        bool hasMore;
        int nextOffset;

        if (IsMe)
        {
            DramaPage page = await _likeService.GetLikesAsync(
                count: 30,
                offset: _offset,
                cancellationToken: ct
            );
            dramaList = page.Items ?? [];
            hasMore = page.HasMore;
            nextOffset = page.NextOffset;
        }
        else
        {
            UserDramaPage page = await _userService.GetUserLikesAsync(
                _userId,
                count: 30,
                offset: _offset,
                cancellationToken: ct
            );
            dramaList = page.Items ?? [];
            hasMore = page.HasMore;
            nextOffset = page.NextOffset;
        }

        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        foreach (Drama item in dramaList)
        {
            Videos.Add(new VideoItemViewModel(item));
        }

        _offset = nextOffset;
        HasMore = hasMore;
    }

    private async Task LoadReviewsDataAsync(long requestId, CancellationToken ct)
    {
        UserWorksQuery query = new()
        {
            SubTab = UserWorkSubTab.Review,
            Cursor = _worksCursor,
            Count = 30,
        };

        UserWorkPage page = await _userService.GetUserWorksAsync(_userId, query, ct);
        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        foreach (UserWork item in page.Items)
        {
            if (item is UserReview review)
            {
                Reviews.Add(new UserReviewItemViewModel(review));
            }
        }

        _worksCursor = page.Cursor;
        HasMore = page.HasMore;
    }

    private async Task LoadFollowsDataAsync(long requestId, CancellationToken ct)
    {
        UserRelationPage page;
        if (IsMe)
        {
            page = await _followService.GetFollowingsAsync(
                cursor: _worksCursor,
                count: 30,
                cancellationToken: ct
            );
        }
        else
        {
            if (string.IsNullOrEmpty(_userId))
            {
                HasMore = false;
                return;
            }

            page = await _userService.GetUserFollowingsAsync(
                _userId,
                cursor: _worksCursor,
                count: 30,
                cancellationToken: ct
            );
        }

        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        foreach (UserRelationItem relation in page.Items)
        {
            if (relation.User != null)
            {
                Relations.Add(new UserItemViewModel(relation));
            }
        }

        _worksCursor = page.Cursor;
        HasMore = page.HasMore;
    }

    private async Task LoadFansDataAsync(long requestId, CancellationToken ct)
    {
        UserRelationPage page;
        if (IsMe)
        {
            page = await _followService.GetFollowersAsync(
                cursor: _worksCursor,
                count: 30,
                cancellationToken: ct
            );
        }
        else
        {
            if (string.IsNullOrEmpty(_userId))
            {
                HasMore = false;
                return;
            }

            page = await _userService.GetUserFollowersAsync(
                _userId,
                cursor: _worksCursor,
                count: 30,
                cancellationToken: ct
            );
        }

        if (requestId != _currentRequestId || ct.IsCancellationRequested)
        {
            return;
        }

        foreach (UserRelationItem relation in page.Items)
        {
            if (relation.User != null)
            {
                Relations.Add(new UserItemViewModel(relation));
            }
        }

        _worksCursor = page.Cursor;
        HasMore = page.HasMore;
    }

    private void CheckEmptyState(UserSpaceSectionItemViewModel section)
    {
        bool hasContent = section.Id switch
        {
            "Works" => Videos.Count > 0 || Reviews.Count > 0,
            "Circle" => CircleFeeds.Count > 0,
            "Reviews" => Reviews.Count > 0,
            "Follows" or "Fans" => Relations.Count > 0,
            _ => Videos.Count > 0,
        };

        IsEmpty = !hasContent;
        if (IsEmpty)
        {
            EmptyDescription = section.Id switch
            {
                "History" => Resources.NoHistoryVideos,
                "Favorite" => Resources.NoFavoriteVideos,
                "ViewLater" => Resources.NoViewLaterVideos,
                "Follows" => Resources.NoFollows,
                "Fans" => Resources.NoFans,
                _ => Resources.NoSpecificData,
            };
        }
    }

    /// <summary>
    /// 在当前空间内搜索/过滤.
    /// </summary>
    public void Search(string keyword) { }

    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        if (string.IsNullOrEmpty(_userId) || IsMe)
        {
            return;
        }

        try
        {
            bool next = !IsFollowed;
            PhoenixReadApi.User? user = _phoenixProfile?.User;
            PhoenixOperationResult res = await _followService.SetFollowAsync(_userId, next, user);
            if (res.Succeeded)
            {
                IsFollowed = next;
                _appViewModel.ShowTip(
                    next ? Resources.Followed : Resources.Unfollowed,
                    InfoType.Success
                );
            }
            else
            {
                _appViewModel.ShowTip(res.Message ?? "关注操作失败", InfoType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换关注状态失败");
            _appViewModel.ShowTip(Resources.FailedToFollowUser, InfoType.Error);
        }
    }

    [RelayCommand]
    private void Pin()
    {
        PinItem pinItem = new(_userId, UserName, AvatarUrl, PinType.User);
        _pinnerViewModel.AddItem(pinItem);
    }

    [RelayCommand]
    public void OpenFollows()
    {
        if (!CanOpenFollows)
        {
            if (!IsFollowsAccessible && !IsMe)
            {
                _appViewModel.ShowTip(Resources.PrivacyProtectedTip, InfoType.Warning);
            }
            return;
        }

        _ = SelectSectionAsync(_followsSection, force: true);
    }

    [RelayCommand]
    public void OpenFans()
    {
        if (!CanOpenFans)
        {
            if (!IsFansAccessible && !IsMe)
            {
                _appViewModel.ShowTip(Resources.PrivacyProtectedTip, InfoType.Warning);
            }
            return;
        }

        _ = SelectSectionAsync(_fansSection, force: true);
    }

    [RelayCommand]
    private void Login()
    {
        StartupWindow startupWindow = new();
        startupWindow.Activate();
    }

    private static UserCard CreateUserCard(UserProfile profile, string name, string avatarUrl)
    {
        UserAvatar? avatar =
            !string.IsNullOrEmpty(avatarUrl)
            && Uri.TryCreate(avatarUrl, UriKind.Absolute, out Uri? uri)
                ? new UserAvatar { Uri = uri }
                : null;

        return new UserCard
        {
            Profile = new UserDetailProfile
            {
                User = new PhoenixReadCopilot.Core.Models.User.User
                {
                    Id = profile.User.Id,
                    Name = name,
                    Avatar = avatar,
                },
                Introduce = profile.User.Introduction,
            },
            Community = new UserCommunity
            {
                FansCount = profile.FansCount,
                FollowCount = profile.FollowCount,
                LikeCount = profile.LikeCount,
            },
        };
    }
}
