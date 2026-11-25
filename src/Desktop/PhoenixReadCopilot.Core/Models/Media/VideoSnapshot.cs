namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 视频快照.
/// </summary>
public sealed class VideoSnapshot(VideoInformation video, bool isPrivate = false)
{
    /// <summary>
    /// 视频信息.
    /// </summary>
    public VideoInformation Video { get; } = video;

    /// <summary>
    /// 无痕播放（不上报历史记录）.
    /// </summary>
    public bool IsPrivate { get; } = isPrivate;
}
