namespace PhoenixReadCopilot.Core.Models.Moment;

/// <summary>
/// 动态发布者概要信息.
/// </summary>
public sealed class MomentProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentProfile"/> class.
    /// </summary>
    public MomentProfile(User.User user, bool isUnread = false)
    {
        User = user;
        IsUnread = isUnread;
    }

    /// <summary>
    /// 用户信息.
    /// </summary>
    public User.User User { get; init; }

    /// <summary>
    /// 是否有未读动态.
    /// </summary>
    public bool IsUnread { get; init; }
}
