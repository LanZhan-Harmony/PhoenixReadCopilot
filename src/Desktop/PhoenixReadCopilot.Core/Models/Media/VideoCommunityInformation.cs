namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 视频社区交互数据.
/// </summary>
public sealed class VideoCommunityInformation
{
    /// <summary>
    /// 播放量.
    /// </summary>
    public double? PlayCount { get; init; }

    /// <summary>
    /// 弹幕数.
    /// </summary>
    public double? DanmakuCount { get; init; }

    /// <summary>
    /// 点赞数.
    /// </summary>
    public double? LikeCount { get; init; }
}
