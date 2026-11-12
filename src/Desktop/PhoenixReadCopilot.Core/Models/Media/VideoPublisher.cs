using PhoenixReadCopilot.Core.Models.User;

namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 视频发布者信息.
/// </summary>
public sealed class VideoPublisher
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPublisher"/> class.
    /// </summary>
    public VideoPublisher(User.User user)
    {
        User = user;
    }

    /// <summary>
    /// 发布者用户.
    /// </summary>
    public User.User User { get; init; }
}
