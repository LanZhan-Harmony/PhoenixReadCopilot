namespace PhoenixReadCopilot.Core.Models.User;

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
