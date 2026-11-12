namespace PhoenixReadCopilot.Core.Models.Moment;

/// <summary>
/// 动态社区交互数据（点赞、评论等）.
/// </summary>
public sealed class MomentCommunityInformation
{
    /// <summary>
    /// 是否已点赞.
    /// </summary>
    public bool IsLiked { get; set; }

    /// <summary>
    /// 点赞数.
    /// </summary>
    public double? LikeCount { get; set; }

    /// <summary>
    /// 评论数.
    /// </summary>
    public double? CommentCount { get; set; }
}
