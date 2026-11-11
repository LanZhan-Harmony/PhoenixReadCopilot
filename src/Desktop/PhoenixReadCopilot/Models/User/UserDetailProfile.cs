namespace PhoenixReadCopilot.Models.User;

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
