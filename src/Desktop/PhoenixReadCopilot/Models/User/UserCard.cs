namespace PhoenixReadCopilot.Models.User;

/// <summary>
/// 用户空间资料卡片.
/// </summary>
public sealed class UserCard
{
    /// <summary>
    /// 用户基础详细资料.
    /// </summary>
    public UserDetailProfile Profile { get; set; } = new();

    /// <summary>
    /// 用户社区数据.
    /// </summary>
    public UserCommunity Community { get; set; } = new();
}
