using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Core.Contracts.Media;

/// <summary>
/// 可播放媒体核心契约接口.
/// 适用于短剧、独立视频、日历上线剧集、历史记录等可被播放器直接播放的视图模型与实体.
/// </summary>
public interface IPlayableMedia
{
    /// <summary>
    /// 媒体唯一标识 (如短剧 ID 或独立视频 ID).
    /// </summary>
    string MediaId { get; }

    /// <summary>
    /// 媒体标题.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// 媒体封面 URL.
    /// </summary>
    string? CoverUrl { get; }

    /// <summary>
    /// 媒体类型 (短剧、独立视频等).
    /// </summary>
    MediaType Type { get; }

    /// <summary>
    /// 起始播放分集 ID / 视频 ID (可选，若为空则由播放器根据历史进度或首集自动决定).
    /// </summary>
    string? InitialEpisodeId => null;

    /// <summary>
    /// 起始播放时间进度 (毫秒，可选).
    /// </summary>
    long? InitialPositionMs => null;

    /// <summary>
    /// 可选关联的短剧基础信息（用于在详情接口返回前 0ms 预填界面）.
    /// </summary>
    Drama? Drama => null;
}
