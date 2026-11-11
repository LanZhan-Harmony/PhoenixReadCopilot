namespace PhoenixReadCopilot.Models.User;

/// <summary>
/// 用户社区数据模型.
/// </summary>
public sealed class UserCommunity
{
    /// <summary>
    /// 粉丝数.
    /// </summary>
    public long FansCount { get; set; }

    /// <summary>
    /// 关注数.
    /// </summary>
    public long FollowCount { get; set; }

    /// <summary>
    /// 获赞数.
    /// </summary>
    public long LikeCount { get; set; }
}
