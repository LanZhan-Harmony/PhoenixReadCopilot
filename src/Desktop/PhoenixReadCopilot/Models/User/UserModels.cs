namespace PhoenixReadCopilot.Models.User;

/// <summary>
/// 用户头像模型.
/// </summary>
public sealed class UserAvatar
{
    /// <summary>
    /// 头像地址.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// 头像源地址 (兼容 XAML 绑定).
    /// </summary>
    public Uri? SourceUri => Uri;

    /// <summary>
    /// 兼容应用图片模型.
    /// </summary>
    public AppImage? AppImage => Uri is not null ? new AppImage(Uri) : null;
}

/// <summary>
/// 用户信息模型.
/// </summary>
public sealed class User
{
    /// <summary>
    /// 用户标识.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 用户名称.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 用户头像.
    /// </summary>
    public UserAvatar? Avatar { get; set; }
}

/// <summary>
/// 用户详细资料模型.
/// </summary>
public sealed class UserDetailProfile
{
    /// <summary>
    /// 用户基础信息.
    /// </summary>
    public User User { get; set; } = new();

    /// <summary>
    /// 个性签名 / 简介.
    /// </summary>
    public string Introduce { get; set; } = string.Empty;

    /// <summary>
    /// 是否为 VIP 会员.
    /// </summary>
    public bool IsVip { get; set; }
}

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
