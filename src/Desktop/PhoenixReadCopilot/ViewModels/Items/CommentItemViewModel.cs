using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Appearance;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 评论项视图模型.
/// </summary>
public sealed partial class CommentItemViewModel : ViewModelBase
{
    private readonly ICommentService _commentService;
    private readonly IAuthenticationService _authService;

    private static readonly ILogger<CommentItemViewModel> _logger = Ioc.Default.GetRequiredService<
        ILogger<CommentItemViewModel>
    >();

    private readonly Action<CommentItemViewModel>? _markReplyTargetAction;

    [ObservableProperty]
    public partial bool IsLiked { get; set; }

    [ObservableProperty]
    public partial int LikeCount { get; set; }

    /// <summary>
    /// 评论唯一标识.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 评论文本正文.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// 子评论/回复条数.
    /// </summary>
    public int ChildCount { get; init; }

    /// <summary>
    /// 发布的相对时间.
    /// </summary>
    public string RelativeTime { get; init; } = string.Empty;

    /// <summary>
    /// 发布的实际时间.
    /// </summary>
    public string ActualTime { get; init; } = string.Empty;

    /// <summary>
    /// 用户头像.
    /// </summary>
    public Uri? Avatar { get; init; }

    /// <summary>
    /// 用户名.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// 用户 ID.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// 所属剧集 ID.
    /// </summary>
    public string? DramaId { get; init; }

    /// <summary>
    /// 所属分集视频 ID.
    /// </summary>
    public string? VideoId { get; init; }

    /// <summary>
    /// 是否为整剧剧评（false 则为分集评论）.
    /// </summary>
    public bool IsDramaReview { get; init; }

    /// <summary>
    /// 是否为红果圈图文帖子评论.
    /// </summary>
    public bool IsCirclePost { get; init; }

    /// <summary>
    /// 是否为二级回复.
    /// </summary>
    public bool IsReply { get; init; }

    /// <summary>
    /// 是否为置顶评论.
    /// </summary>
    public bool IsTop { get; init; }

    /// <summary>
    /// 数据本身引用（保留向后兼容）.
    /// </summary>
    public CommentItemViewModel Data => this;

    /// <summary>
    /// 富文本内容（包含文字和附图）.
    /// </summary>
    public EmoteText Content { get; init; }

    /// <summary>
    /// 红果评论构造函数（分集评论或整剧剧评）.
    /// </summary>
    public CommentItemViewModel(
        Comment comment,
        string dramaId,
        string? videoId,
        ICommentService? commentService = null,
        IAuthenticationService? authService = null,
        bool isDramaReview = false,
        Action<CommentItemViewModel>? markReplyTargetAction = null,
        bool isCirclePost = false
    )
    {
        Id = comment.Id;
        Text = comment.Text;
        DramaId = dramaId;
        VideoId = videoId;
        IsDramaReview = isDramaReview;
        IsCirclePost = isCirclePost;
        IsReply = false;
        _commentService = commentService ?? ItemServices.Comment;
        _authService = authService ?? Ioc.Default.GetRequiredService<IAuthenticationService>();
        _markReplyTargetAction = markReplyTargetAction;

        IsTop = false;
        IsLiked = false;
        LikeCount = Convert.ToInt32(comment.LikeCount);
        ChildCount = Convert.ToInt32(comment.ReplyCount);

        UserName = comment.Author?.Name ?? "匿名观众";
        UserId = comment.Author?.Id ?? string.Empty;

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

        if (
            !string.IsNullOrEmpty(comment.Author?.AvatarUrl)
            && Uri.TryCreate(comment.Author.AvatarUrl, UriKind.Absolute, out Uri? avatarUri)
        )
        {
            Avatar = avatarUri;
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

    /// <summary>
    /// 红果二级评论回复构造函数.
    /// </summary>
    public CommentItemViewModel(
        CommentReply reply,
        string dramaId,
        string? videoId,
        ICommentService? commentService = null,
        IAuthenticationService? authService = null,
        bool isDramaReview = false,
        Action<CommentItemViewModel>? markReplyTargetAction = null,
        bool isCirclePost = false
    )
    {
        Id = reply.Id;
        Text = reply.Text;
        DramaId = dramaId;
        VideoId = videoId;
        IsDramaReview = isDramaReview;
        IsCirclePost = isCirclePost;
        IsReply = true;
        _commentService = commentService ?? ItemServices.Comment;
        _authService = authService ?? Ioc.Default.GetRequiredService<IAuthenticationService>();
        _markReplyTargetAction = markReplyTargetAction;

        IsTop = false;
        IsLiked = false;
        LikeCount = Convert.ToInt32(reply.LikeCount);
        ChildCount = 0;

        UserName = reply.Author?.Name ?? "匿名观众";
        UserId = reply.Author?.Id ?? string.Empty;

        if (reply.CreatedAtUnixSeconds > 0)
        {
            DateTimeOffset time = DateTimeOffset
                .FromUnixTimeSeconds(reply.CreatedAtUnixSeconds)
                .ToLocalTime();
            RelativeTime = time.Humanize();
            ActualTime = time.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
        {
            RelativeTime = "刚刚";
            ActualTime = string.Empty;
        }

        if (
            !string.IsNullOrEmpty(reply.Author?.AvatarUrl)
            && Uri.TryCreate(reply.Author.AvatarUrl, UriKind.Absolute, out Uri? avatarUri)
        )
        {
            Avatar = avatarUri;
        }

        List<AppImage>? pictures =
            reply.Images?.Count > 0
                ?
                [
                    .. reply
                        .Images.Where(img =>
                            !string.IsNullOrEmpty(img.Url)
                            && Uri.TryCreate(img.Url, UriKind.Absolute, out _)
                        )
                        .Select(img => new AppImage(img)),
                ]
                : null;
        Content = new EmoteText(reply.Text, pictures);
    }

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        if (!_authService.IsAuthenticated)
        {
            return;
        }

        bool state = !IsLiked;
        try
        {
            PhoenixOperationResult res;
            if (IsCirclePost)
            {
                string? targetId = !string.IsNullOrEmpty(VideoId) ? VideoId : DramaId;
                if (string.IsNullOrEmpty(targetId))
                {
                    return;
                }
                res = IsReply
                    ? await _commentService.LikeCirclePostReplyAsync(Id, targetId, state)
                    : await _commentService.LikeCirclePostCommentAsync(Id, targetId, state);
            }
            else if (IsReply)
            {
                string? targetId = !string.IsNullOrEmpty(VideoId) ? VideoId : DramaId;
                if (string.IsNullOrEmpty(targetId))
                {
                    return;
                }
                res = await _commentService.SetReplyLikeAsync(Id, targetId, state);
            }
            else if (IsDramaReview && !string.IsNullOrEmpty(DramaId))
            {
                res = await _commentService.SetDramaReviewLikeAsync(Id, DramaId, state);
            }
            else
            {
                string? targetId = !string.IsNullOrEmpty(VideoId) ? VideoId : DramaId;
                if (string.IsNullOrEmpty(targetId))
                {
                    return;
                }
                res = await _commentService.SetCommentLikeAsync(Id, targetId, state);
            }

            if (res.Succeeded)
            {
                IsLiked = state;
                LikeCount += state ? 1 : -1;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"点赞评论失败");
        }
    }

    [RelayCommand]
    private void MarkReplyTarget() => _markReplyTargetAction?.Invoke(this);

    [RelayCommand]
    private void ShowUserSpace()
    {
        // 红果短剧暂无读者空间，预留空命令以防 XAML 绑定失败
    }
}
