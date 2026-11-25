using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 视频/短剧信息模型.
/// </summary>
public sealed class VideoInformation(
    MediaIdentifier identifier,
    double? duration = null,
    DateTime? publishTime = null,
    double? danmakuCount = null,
    double? playCount = null,
    VideoPublisher? publisher = null,
    VideoCommunityInformation? communityInformation = null
)
{
    /// <summary>
    /// 基础媒体标识.
    /// </summary>
    public MediaIdentifier Identifier { get; init; } = identifier;

    /// <summary>
    /// 时长（秒）.
    /// </summary>
    public double? Duration { get; init; } = duration;

    /// <summary>
    /// 发布时间.
    /// </summary>
    public DateTime? PublishTime { get; init; } = publishTime;

    /// <summary>
    /// 弹幕数.
    /// </summary>
    public double? DanmakuCount { get; init; } = danmakuCount;

    /// <summary>
    /// 播放量.
    /// </summary>
    public double? PlayCount { get; init; } = playCount;

    /// <summary>
    /// 发布者信息.
    /// </summary>
    public VideoPublisher? Publisher { get; init; } = publisher;

    /// <summary>
    /// 社区互动数据.
    /// </summary>
    public VideoCommunityInformation? CommunityInformation { get; init; } = communityInformation;

    /// <summary>
    /// 扩展数据字典.
    /// </summary>
    public Dictionary<string, object> ExtensionData { get; init; } = [];

    /// <summary>
    /// 获取扩展属性.
    /// </summary>
    public T? GetExtensionIfNotNull<T>(VideoExtensionDataId id) =>
        GetExtensionIfNotNull<T>(id.ToString());

    /// <summary>
    /// 获取扩展属性.
    /// </summary>
    public T? GetExtensionIfNotNull<T>(string key)
    {
        if (ExtensionData.TryGetValue(key, out object? value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }
}
