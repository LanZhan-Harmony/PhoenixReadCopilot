namespace PhoenixReadCopilot.Core.Models.Player;

/// <summary>
/// 播放格式与画质信息.
/// </summary>
/// <param name="Quality">画质得分或分辨率.</param>
/// <param name="Description">画质文本描述.</param>
/// <param name="NeedVip">是否需要 VIP 权限.</param>
/// <param name="NeedLogin">是否需要登录.</param>
public sealed record PlayerFormatInformation(
    int Quality,
    string Description,
    bool NeedVip = false,
    bool NeedLogin = false
);
