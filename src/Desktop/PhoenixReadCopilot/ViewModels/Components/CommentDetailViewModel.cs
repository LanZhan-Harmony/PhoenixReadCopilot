using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 评论详情视图模型.
/// </summary>
internal sealed partial class CommentDetailViewModel : ViewModelBase
{
    private readonly ILogger<CommentDetailViewModel> _logger;
    private readonly Action<CommentDetailViewModel>? _showMoreAction;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ICommentService _commentService;
    private readonly IAuthenticationService _authService;
    private readonly INotificationService? _notificationService;
    private readonly string? _dramaId;
    private readonly string? _videoId;

    /// <summary>
    /// 顶级主评论视图模型.
    /// </summary>
    public CommentItemViewModel Data { get; }

    private bool _preventLoadMore;
    private string _cursor = string.Empty;
    private CommentItemViewModel? _replyItem;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ReplyTarget { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsReplying { get; set; }

    /// <summary>
    /// 列表已更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <summary>
    /// 评论回复列表.
    /// </summary>
    public ObservableCollection<CommentItemViewModel> Comments { get; } = [];

    /// <summary>
    /// 红果短剧评论详情构造函数.
    /// </summary>
    public CommentDetailViewModel(
        CommentItemViewModel data,
        Action<CommentDetailViewModel>? showMoreAction,
        DispatcherQueue dispatcherQueue,
        ILogger<CommentDetailViewModel> logger,
        ICommentService commentService,
        IAuthenticationService authService,
        INotificationService? notificationService,
        string? dramaId,
        string? videoId
    )
    {
        Data = data;
        _showMoreAction = showMoreAction;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
        _commentService = commentService;
        _authService = authService;
        _notificationService = notificationService;
        _dramaId = dramaId;
        _videoId = videoId;
    }

    [RelayCommand]
    public void Initialize()
    {
        if (Comments.Count > 0)
        {
            return;
        }

        _ = LoadItemsAsync();
    }

    public async Task LoadItemsAsync()
    {
        if (IsLoading || _preventLoadMore || IsEmpty)
        {
            return;
        }

        string? effectiveDramaId = !string.IsNullOrEmpty(_dramaId) ? _dramaId : Data.DramaId;
        if (string.IsNullOrEmpty(effectiveDramaId))
        {
            return;
        }

        IsLoading = true;
        try
        {
            CommentReplyPage replyPage;

            // 圈子帖子回复、剧评回复还是分集评论回复
            if (Data.IsCirclePost)
            {
                string postId = !string.IsNullOrEmpty(_videoId) ? _videoId : effectiveDramaId;
                replyPage = await _commentService.GetCirclePostCommentRepliesAsync(
                    commentId: Data.Id,
                    postId: postId,
                    count: 20,
                    cursor: _cursor
                );
            }
            else if (Data.IsDramaReview || string.IsNullOrEmpty(_videoId))
            {
                replyPage = await _commentService.GetDramaReviewRepliesAsync(
                    commentId: Data.Id,
                    dramaId: effectiveDramaId,
                    count: 20,
                    cursor: _cursor
                );
            }
            else
            {
                replyPage = await _commentService.GetEpisodeCommentRepliesAsync(
                    commentId: Data.Id,
                    dramaId: effectiveDramaId,
                    videoId: _videoId,
                    count: 20,
                    cursor: _cursor
                );
            }

            _cursor = replyPage.Cursor;
            _preventLoadMore = !replyPage.HasMore;

            foreach (CommentReply item in replyPage.Items)
            {
                if (Comments.Any(p => p.Id == item.Id))
                {
                    continue;
                }

                Comments.Add(
                    new CommentItemViewModel(
                        item,
                        effectiveDramaId,
                        _videoId,
                        _commentService,
                        _authService,
                        isDramaReview: Data.IsDramaReview,
                        markReplyTargetAction: SetReplyTarget,
                        isCirclePost: Data.IsCirclePost
                    )
                );
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"加载红果评论回复详情时出错");
            _preventLoadMore = true;
        }
        finally
        {
            IsEmpty = Comments.Count == 0;
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        Clear();
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
            await LoadItemsAsync()
        );
    }

    public void Clear()
    {
        Comments.Clear();
        _cursor = string.Empty;
        _preventLoadMore = false;
        IsEmpty = false;
        ResetReplyTarget();
    }

    [RelayCommand]
    private void ShowMore()
    {
        _showMoreAction?.Invoke(this);
        Initialize();
    }

    [RelayCommand]
    private async Task SendReplyAsync(string content)
    {
        if (IsReplying || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        content = EmojiHelper.FormatEmojiText(content.Trim());

        if (!_authService.IsAuthenticated)
        {
            _notificationService?.ShowInAppNotification(
                "请先登录后再发表回复",
                NotificationType.Warning
            );
            return;
        }

        string? effectiveDramaId = !string.IsNullOrEmpty(_dramaId) ? _dramaId : Data.DramaId;
        if (string.IsNullOrEmpty(effectiveDramaId))
        {
            return;
        }

        IsReplying = true;
        try
        {
            string targetUserId = _replyItem?.UserId ?? Data.UserId;
            PhoenixOperationResult res;

            if (Data.IsDramaReview || string.IsNullOrEmpty(_videoId))
            {
                // 回复整剧剧评
                res = await _commentService.SendDramaReviewReplyAsync(
                    commentId: Data.Id,
                    dramaId: effectiveDramaId,
                    text: content,
                    replyToUserId: targetUserId
                );
            }
            else
            {
                // 回复分集评论
                res = await _commentService.SendEpisodeCommentReplyAsync(
                    commentId: Data.Id,
                    dramaId: effectiveDramaId,
                    videoId: _videoId,
                    text: content,
                    replyToUserId: targetUserId
                );
            }

            if (res.Succeeded)
            {
                _notificationService?.ShowInAppNotification("回复已发送", NotificationType.Success);
                ResetReplyTarget();
                Refresh();
            }
            else
            {
                _notificationService?.ShowInAppNotification(
                    res.Message ?? "发送失败",
                    NotificationType.Error
                );
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"发送红果评论回复失败");
            _notificationService?.ShowInAppNotification("发送回复失败", NotificationType.Error);
        }
        finally
        {
            IsReplying = false;
        }
    }

    [RelayCommand]
    private void ResetReplyTarget()
    {
        _replyItem = null;
        ReplyTarget = string.Empty;
    }

    private void SetReplyTarget(CommentItemViewModel item)
    {
        _replyItem = item;
        ReplyTarget = item?.UserName ?? string.Empty;
    }
}
