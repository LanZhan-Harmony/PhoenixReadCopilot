using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Media;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 通用可播放媒体导航参数.
/// </summary>
public sealed record PlayableMedia(
    string MediaId,
    string Title = "",
    string? CoverUrl = null,
    MediaType Type = MediaType.Drama,
    string? InitialEpisodeId = null,
    long? InitialPositionMs = null,
    Drama? Drama = null
) : IPlayableMedia;
