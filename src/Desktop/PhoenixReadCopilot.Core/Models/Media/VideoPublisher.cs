namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 视频发布者信息.
/// </summary>
public sealed class VideoPublisher(User.User user)
{
    /// <summary>
    /// 发布者用户.
    /// </summary>
    public User.User User { get; init; } = user;
}
