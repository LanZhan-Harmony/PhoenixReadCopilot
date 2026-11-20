namespace PhoenixReadCopilot.Core.Models.User;

/// <summary>
/// 用户空间页面导航参数.
/// </summary>
/// <param name="UserId">目标用户 ID，若为 null 则表示当前登录用户（“我的”主页）.</param>
/// <param name="TargetSection">指定初始选中的 Section ID（例如 History, Favorite, ViewLater, Reservation, Works, Circle, Follows, Fans）.</param>
public sealed record UserSpaceNavArgs(string? UserId = null, string? TargetSection = null);
