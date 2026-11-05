using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Pages.Overlay;
using Richasy.BiliKernel.Bili.Comment;
using Richasy.BiliKernel.Models.Appearance;
using Richasy.BiliKernel.Models.Comment;
using Richasy.BiliKernel.Models.User;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 评论项视图模型.
/// </summary>
public sealed partial class CommentItemViewModel : ViewModelBase<CommentInformation>
{
    private readonly PhoenixReadApiClient? _apiClient;
    private readonly string? _seriesId;
    private readonly string? _videoId;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentItemViewModel"/> class.
    /// </summary>
    public CommentItemViewModel(
        CommentInformation data,
        ICommentService service,
        Action<CommentItemViewModel> markReplyTargetAction
    )
        : base(data)
    {
        _service = service;
        IsTop = data.IsTop;
        IsLiked = data.CommunityInformation?.IsLiked ?? false;
        LikeCount = Convert.ToInt32(data.CommunityInformation?.LikeCount ?? 0);
        RelativeTime = data.PublishTime.Humanize();
        ActualTime = data.PublishTime.ToString("yyyy-MM-dd HH:mm:ss");
        ChildCount = data.CommunityInformation?.ChildCount ?? 0;
        Avatar = data.User?.User?.Avatar?.Uri;
        UserName = data.User?.User?.Name ?? string.Empty;
        Content = data.Content;
        _markReplyTargetAction = markReplyTargetAction;
    }

    /// <summary>
    /// 红果分集评论构造函数.
    /// </summary>
    public CommentItemViewModel(
        DramaComment comment,
        string seriesId,
        string videoId,
        PhoenixReadApiClient apiClient,
        Action<CommentItemViewModel>? markReplyTargetAction = default
    )
        : base(
            CreateCommentInfo(
                comment.Id,
                comment.Text,
                comment.Author?.Id ?? string.Empty,
                comment.Author?.Name ?? "匿名观众",
                Uri.TryCreate(comment.Author?.AvatarUrl, UriKind.Absolute, out var uri)
                    ? uri
                    : default,
                comment.CreatedAtUnixSeconds,
                Convert.ToInt32(comment.LikeCount),
                Convert.ToInt32(comment.ReplyCount)
            )
        )
    {
        _seriesId = seriesId;
        _videoId = videoId;
        _apiClient = apiClient;
        _markReplyTargetAction = markReplyTargetAction;

        IsTop = false;
        IsLiked = false;
        LikeCount = Convert.ToInt32(comment.LikeCount);
        ChildCount = Convert.ToInt32(comment.ReplyCount);

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

        UserName = comment.Author?.Name ?? "匿名观众";
        if (
            !string.IsNullOrEmpty(comment.Author?.AvatarUrl)
            && Uri.TryCreate(comment.Author.AvatarUrl, UriKind.Absolute, out var avatarUri)
        )
        {
            Avatar = avatarUri;
        }

        Content = new EmoteText(comment.Text, null, null);
    }

    /// <summary>
    /// 红果评论回复构造函数.
    /// </summary>
    public CommentItemViewModel(
        CommentReply reply,
        string seriesId,
        string videoId,
        PhoenixReadApiClient apiClient,
        Action<CommentItemViewModel>? markReplyTargetAction = default
    )
        : base(
            CreateCommentInfo(
                reply.Id,
                reply.Text,
                reply.Author?.Id ?? string.Empty,
                reply.Author?.Name ?? "匿名观众",
                Uri.TryCreate(reply.Author?.AvatarUrl, UriKind.Absolute, out var uri)
                    ? uri
                    : default,
                reply.CreatedAtUnixSeconds,
                Convert.ToInt32(reply.LikeCount),
                0
            )
        )
    {
        _seriesId = seriesId;
        _videoId = videoId;
        _apiClient = apiClient;
        _markReplyTargetAction = markReplyTargetAction;

        IsTop = false;
        IsLiked = false;
        LikeCount = Convert.ToInt32(reply.LikeCount);
        ChildCount = 0;

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

        UserName = reply.Author?.Name ?? "匿名观众";
        if (
            !string.IsNullOrEmpty(reply.Author?.AvatarUrl)
            && Uri.TryCreate(reply.Author.AvatarUrl, UriKind.Absolute, out var avatarUri)
        )
        {
            Avatar = avatarUri;
        }

        Content = new EmoteText(reply.Text, null, null);
    }

    private static CommentInformation CreateCommentInfo(
        string id,
        string text,
        string userId,
        string userName,
        Uri? avatar,
        long createdSeconds,
        int likes,
        int replies
    )
    {
        var avatarImage = avatar != null ? new BiliImage(avatar) : default;
        var userProfile = new Richasy.BiliKernel.Models.User.UserProfile(
            userId,
            userName,
            avatarImage
        );
        var userDetail = new UserDetailProfile(userProfile, default, default, default, default);
        var emote = new EmoteText(text, null, null);
        var pubTime =
            createdSeconds > 0
                ? DateTimeOffset.FromUnixTimeSeconds(createdSeconds)
                : DateTimeOffset.Now;
        var community = new CommentCommunityInformation(id, (double)likes, replies, false);
        return new CommentInformation(
            id,
            emote,
            string.Empty,
            false,
            userDetail,
            pubTime,
            community
        );
    }

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        var state = !IsLiked;
        if (_apiClient is not null && !string.IsNullOrEmpty(_videoId))
        {
            try
            {
                var res = await _apiClient.SetCommentLikeAsync(Data.Id, _videoId, state);
                if (res.Succeeded)
                {
                    IsLiked = state;
                    LikeCount += state ? 1 : -1;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "点赞红果评论失败");
            }
            return;
        }

        try
        {
            if (_service is not null)
            {
                await _service.ToggleLikeAsync(state, Data.Id, Data.CommentId, Data.CommentType);
                IsLiked = state;
                LikeCount += state ? 1 : -1;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "切换评论点赞状态时失败");
        }
    }

    [RelayCommand]
    private void MarkReplyTarget() => _markReplyTargetAction?.Invoke(this);

    [RelayCommand]
    private void ShowUserSpace()
    {
        if (Data.User?.User is not null)
        {
            ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Data.User.User);
        }
    }
}
