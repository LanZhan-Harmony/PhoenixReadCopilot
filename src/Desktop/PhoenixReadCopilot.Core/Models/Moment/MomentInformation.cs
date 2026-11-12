using PhoenixReadCopilot.Core.Models.Appearance;

namespace PhoenixReadCopilot.Core.Models.Moment;

/// <summary>
/// 动态条目数据模型.
/// </summary>
public sealed class MomentInformation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentInformation"/> class.
    /// </summary>
    public MomentInformation(
        string id,
        User.User? user = null,
        MomentCommunityInformation? communityInformation = null,
        string? tip = null,
        EmoteText? description = null,
        object? data = null,
        string? commentId = null
    )
    {
        Id = id;
        User = user;
        CommunityInformation = communityInformation;
        Tip = tip;
        Description = description;
        Data = data;
        CommentId = commentId ?? id;
    }

    /// <summary>
    /// 动态唯一标识.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// 评论标识.
    /// </summary>
    public string? CommentId { get; init; }

    /// <summary>
    /// 发布者信息.
    /// </summary>
    public User.User? User { get; init; }

    /// <summary>
    /// 社区互动信息.
    /// </summary>
    public MomentCommunityInformation? CommunityInformation { get; init; }

    /// <summary>
    /// 提示/副标题信息.
    /// </summary>
    public string? Tip { get; init; }

    /// <summary>
    /// 动态描述/正文.
    /// </summary>
    public EmoteText? Description { get; init; }

    /// <summary>
    /// 嵌入数据（视频、图片列表或嵌套转发动态等）.
    /// </summary>
    public object? Data { get; init; }
}
