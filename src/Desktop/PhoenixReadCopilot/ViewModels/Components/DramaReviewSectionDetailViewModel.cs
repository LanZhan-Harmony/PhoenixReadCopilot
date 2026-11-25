using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 剧评列表项视图模型.
/// </summary>
internal sealed partial class DramaReviewItemViewModel : ViewModelBase
{
    private readonly ICommentService _commentService;
    private readonly IAuthenticationService _authService;
    private readonly INotificationService _notificationService;
    private readonly ILogger _logger;
    private readonly string _dramaId;
    private readonly Action<DramaReviewItemViewModel>? _showMoreAction;

    /// <summary>
    /// 评论数据实体.
    /// </summary>
    public Comment Data { get; }

    public DramaReviewItemViewModel(
        DramaReview review,
        string dramaId,
        ICommentService commentService,
        IAuthenticationService authService,
        INotificationService notificationService,
        ILogger logger,
        Action<DramaReviewItemViewModel>? showMoreAction = null
    )
        : this(
            new Comment
            {
                Id = review.Id,
                Text = review.Text,
                Author = review.Author,
                CreatedAtUnixSeconds = review.CreatedAtUnixSeconds,
                LikeCount = review.LikeCount,
                ReplyCount = review.ReplyCount,
                Images = review.Images,
            },
            dramaId,
            commentService,
            authService,
            notificationService,
            logger,
            showMoreAction
        ) { }

    public DramaReviewItemViewModel(
        Comment comment,
        string dramaId,
        ICommentService commentService,
        IAuthenticationService authService,
        INotificationService notificationService,
        ILogger logger,
        Action<DramaReviewItemViewModel>? showMoreAction = null
    )
    {
        Data = comment;
        _dramaId = dramaId;
        _commentService = commentService;
        _authService = authService;
        _notificationService = notificationService;
        _logger = logger;
        _showMoreAction = showMoreAction;

        Id = comment.Id;
        Text = comment.Text;
        UserName = comment.Author?.Name ?? "匿名观众";
        if (
            !string.IsNullOrEmpty(comment.Author?.AvatarUrl)
            && Uri.TryCreate(comment.Author.AvatarUrl, UriKind.Absolute, out Uri? avatarUri)
        )
        {
            Avatar = avatarUri;
        }

        LikeCount = (int)comment.LikeCount;
        ReplyCount = (int)comment.ReplyCount;

        if (comment.CreatedAtUnixSeconds > 0)
        {
            DateTimeOffset time = DateTimeOffset
                .FromUnixTimeSeconds(comment.CreatedAtUnixSeconds)
                .ToLocalTime();
            RelativeTime = time.Humanize();
            ActualTime = time.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
        {
            RelativeTime = "刚刚";
            ActualTime = string.Empty;
        }

        List<AppImage>? pictures =
            comment.Images?.Count > 0
                ?
                [
                    .. comment
                        .Images.Where(img =>
                            !string.IsNullOrEmpty(img.Url)
                            && Uri.TryCreate(img.Url, UriKind.Absolute, out _)
                        )
                        .Select(img => new AppImage(img)),
                ]
                : null;
        Content = new EmoteText(comment.Text, pictures);
    }

    public string Id { get; }
    public string Text { get; }
    public EmoteText Content { get; }
    public string UserName { get; }
    public Uri? Avatar { get; }
    public string RelativeTime { get; }
    public string ActualTime { get; }
    public int ReplyCount { get; }

    [ObservableProperty]
    public partial bool IsLiked { get; set; }

    [ObservableProperty]
    public partial int LikeCount { get; set; }

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            _notificationService.ShowInAppNotification(
                "请先登录后再点赞",
                NotificationType.Warning
            );
            return;
        }

        try
        {
            bool next = !IsLiked;
            PhoenixOperationResult res = await _commentService.SetDramaReviewLikeAsync(
                Id,
                _dramaId,
                next
            );
            if (res.Succeeded)
            {
                IsLiked = next;
                LikeCount += next ? 1 : -1;
            }
            else
            {
                _notificationService.ShowInAppNotification(
                    res.Message ?? "点赞失败",
                    NotificationType.Error
                );
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"点赞剧评失败.");
        }
    }

    [RelayCommand]
    private void ShowMore() => _showMoreAction?.Invoke(this);
}

/// <summary>
/// 剧评区块视图模型 (整部剧只有一份).
/// </summary>
internal sealed partial class DramaReviewSectionDetailViewModel(
    ICommentService commentService,
    IAuthenticationService authService,
    INotificationService notificationService,
    DispatcherQueue dispatcherQueue,
    ILogger<DramaReviewSectionDetailViewModel> logger
) : ViewModelBase, IPlayerSectionDetailViewModel
{
    private readonly ICommentService _commentService = commentService;
    private readonly IAuthenticationService _authService = authService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly ILogger<DramaReviewSectionDetailViewModel> _logger = logger;

    private string _cursor = string.Empty;
    private bool _hasMore = true;

    /// <inheritdoc/>
    public string Title => "剧评";

    [ObservableProperty]
    public partial string DramaId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DramaScore { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Rating { get; set; } = 5.0; // 默认 5 星满分

    [ObservableProperty]
    public partial bool IsLoggedIn { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSending { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial long TotalCount { get; set; }

    [ObservableProperty]
    public partial List<CommentSortOrder> Sorts { get; set; } =
    [CommentSortOrder.Hot, CommentSortOrder.Latest];

    [ObservableProperty]
    public partial CommentSortOrder SortType { get; set; } = CommentSortOrder.Hot;

    [ObservableProperty]
    public partial CommentDetailViewModel? SelectedItem { get; set; }

    /// <summary>
    /// 是否有更多剧评可加载.
    /// </summary>
    public bool HasMore => _hasMore;

    /// <summary>
    /// 列表已更新事件.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <summary>
    /// 初始化事件.
    /// </summary>
    public event EventHandler? Initialized;

    public ObservableCollection<DramaReviewItemViewModel> Reviews { get; } = [];

    public bool CanSendReview => IsLoggedIn && !IsSending;

    public string ReviewPlaceholderText =>
        IsLoggedIn ? "发表你对本剧的评价..." : "请先登录后发表剧评";

    /// <summary>
    /// 初始化剧评组件.
    /// </summary>
    public void Initialize(string dramaId, string score)
    {
        DramaId = dramaId;
        DramaScore = score;
        UpdateLoginState();
        Clear();
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateLoginState()
    {
        IsLoggedIn = _authService.IsAuthenticated;
        OnPropertyChanged(nameof(CanSendReview));
        OnPropertyChanged(nameof(ReviewPlaceholderText));
    }

    public async Task TryFirstLoadAsync()
    {
        if (Reviews.Count > 0 || string.IsNullOrEmpty(DramaId))
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        Clear();
        await LoadItemsAsync();
    }

    public async Task ChangeSortTypeAsync(CommentSortOrder type)
    {
        SortType = type;
        await RefreshAsync();
    }

    public async Task LoadMoreAsync()
    {
        if (!_hasMore || IsLoading)
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task SendReviewAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || IsSending || string.IsNullOrEmpty(DramaId))
        {
            return;
        }

        if (!_authService.IsAuthenticated)
        {
            _notificationService.ShowInAppNotification(
                "请先登录后再发表剧评",
                NotificationType.Warning
            );
            return;
        }

        IsSending = true;
        OnPropertyChanged(nameof(CanSendReview));
        string formatted = EmojiHelper.FormatEmojiText(content.Trim());
        try
        {
            double sendRating = Math.Clamp(Math.Round(Rating * 2.0) / 2.0, 0.5, 5.0);
            PhoenixOperationResult result = await _commentService.SendDramaReviewAsync(
                DramaId,
                sendRating,
                formatted
            );
            if (result.Succeeded)
            {
                _notificationService.ShowInAppNotification(
                    "剧评发表成功",
                    NotificationType.Success
                );
                await RefreshAsync();
            }
            else
            {
                _notificationService.ShowInAppNotification(
                    result.Message ?? "发表失败",
                    NotificationType.Error
                );
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"发送剧评失败.");
            _notificationService.ShowInAppNotification("发送剧评失败", NotificationType.Error);
        }
        finally
        {
            IsSending = false;
            OnPropertyChanged(nameof(CanSendReview));
        }
    }

    [RelayCommand]
    private void BackToMain() => SelectedItem = null;

    private void ShowReviewDetail(DramaReviewItemViewModel item)
    {
        CommentItemViewModel commentItemVm = new(
            item.Data,
            DramaId,
            videoId: null,
            _commentService,
            _authService,
            isDramaReview: true
        );

        SelectedItem = new CommentDetailViewModel(
            commentItemVm,
            showMoreAction: _ => { },
            _dispatcherQueue,
            Ioc.Default.GetRequiredService<ILogger<CommentDetailViewModel>>(),
            _commentService,
            _authService,
            _notificationService,
            DramaId,
            videoId: null
        );
        SelectedItem.Initialize();
    }

    private async Task LoadItemsAsync()
    {
        if (string.IsNullOrEmpty(DramaId) || IsLoading)
        {
            return;
        }

        IsLoading = true;
        UpdateLoginState();
        try
        {
            DramaReviewPage page = await _commentService.GetDramaReviewsAsync(
                DramaId,
                count: 20,
                cursor: _cursor,
                sort: SortType
            );
            _cursor = page.Cursor;
            _hasMore = page.HasMore;
            TotalCount = page.Total;

            foreach (DramaReview item in page.Items)
            {
                if (!Reviews.Any(r => r.Id == item.Id))
                {
                    Reviews.Add(
                        new DramaReviewItemViewModel(
                            item,
                            DramaId,
                            _commentService,
                            _authService,
                            _notificationService,
                            _logger,
                            ShowReviewDetail
                        )
                    );
                }
            }

            IsEmpty = Reviews.Count == 0;
            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"加载剧评失败.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Clear()
    {
        _cursor = string.Empty;
        _hasMore = true;
        Reviews.Clear();
        IsEmpty = false;
        SelectedItem = null;
    }
}
