using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 评论项视图模型.
/// </summary>
public sealed partial class CommentItemViewModel : ViewModelBase
{
    private readonly PhoenixReadApiClient? _apiClient;

    /// <summary>
    /// 红果评论构造函数（分集评论或整剧剧评）.
    /// </summary>
    public CommentItemViewModel(
        DramaComment comment,
        string seriesId,
        string? videoId,
        PhoenixReadApiClient apiClient,
        bool isDramaReview = false,
        Action<CommentItemViewModel>? markReplyTargetAction = default
    )
    {
        Id = comment.Id;
        Text = comment.Text;
        SeriesId = seriesId;
        VideoId = videoId;
        IsDramaReview = isDramaReview;
        _apiClient = apiClient;
        _markReplyTargetAction = markReplyTargetAction;

        IsTop = false;
        IsLiked = false;
        LikeCount = Convert.ToInt32(comment.LikeCount);
        ChildCount = Convert.ToInt32(comment.ReplyCount);

        UserName = comment.Author?.Name ?? "匿名观众";
        UserId = comment.Author?.Id ?? string.Empty;

        if (comment.CreatedAtUnixSeconds > 0)
        {
            var time = DateTimeOffset
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
            && Uri.TryCreate(comment.Author.AvatarUrl, UriKind.Absolute, out var avatarUri)
        )
        {
            Avatar = avatarUri;
        }
    }

    /// <summary>
    /// 红果二级评论回复构造函数.
    /// </summary>
    public CommentItemViewModel(
        CommentReply reply,
        string seriesId,
        string? videoId,
        PhoenixReadApiClient apiClient,
        bool isDramaReview = false,
        Action<CommentItemViewModel>? markReplyTargetAction = default
    )
    {
        Id = reply.Id;
        Text = reply.Text;
        SeriesId = seriesId;
        VideoId = videoId;
        IsDramaReview = isDramaReview;
        _apiClient = apiClient;
        _markReplyTargetAction = markReplyTargetAction;

        IsTop = false;
        IsLiked = false;
        LikeCount = Convert.ToInt32(reply.LikeCount);
        ChildCount = 0;

        UserName = reply.Author?.Name ?? "匿名观众";
        UserId = reply.Author?.Id ?? string.Empty;

        if (reply.CreatedAtUnixSeconds > 0)
        {
            var time = DateTimeOffset.FromUnixTimeSeconds(reply.CreatedAtUnixSeconds).ToLocalTime();
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
            && Uri.TryCreate(reply.Author.AvatarUrl, UriKind.Absolute, out var avatarUri)
        )
        {
            Avatar = avatarUri;
        }
    }

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        if (_apiClient is null)
        {
            return;
        }

        if (!_apiClient.GetAuthenticationState().IsAuthenticated)
        {
            return;
        }

        var state = !IsLiked;
        try
        {
            PhoenixOperationResult res;
            if (IsDramaReview && !string.IsNullOrEmpty(SeriesId))
            {
                res = await _apiClient.SetReviewLikeAsync(Id, SeriesId, state);
            }
            else
            {
                var targetId = !string.IsNullOrEmpty(VideoId) ? VideoId : SeriesId;
                if (string.IsNullOrEmpty(targetId))
                {
                    return;
                }
                res = await _apiClient.SetCommentLikeAsync(Id, targetId, state);
            }

            if (res.Succeeded)
            {
                IsLiked = state;
                LikeCount += state ? 1 : -1;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "点赞评论失败");
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
