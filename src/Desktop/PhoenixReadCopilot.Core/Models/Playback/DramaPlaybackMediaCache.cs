using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Models.Playback;

/// <summary>
/// 剧集播放媒体缓存模型（每部剧对应一个独立的本地持久化文件）.
/// </summary>
public sealed class DramaPlaybackMediaCache
{
    /// <summary>
    /// 短剧 ID.
    /// </summary>
    public string DramaId { get; set; } = string.Empty;

    /// <summary>
    /// 最近更新时间戳（Unix 秒）.
    /// </summary>
    public long LastUpdatedAtUnixSeconds { get; set; }

    /// <summary>
    /// 各分集解析出的多清晰度媒体列表（键为分集 videoId）.
    /// </summary>
    public Dictionary<string, List<PlaybackMedia>> Episodes { get; set; } = [];
}
