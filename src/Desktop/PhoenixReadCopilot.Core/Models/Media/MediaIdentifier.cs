using PhoenixReadCopilot.Core.Models.Appearance;

namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 媒体基础标识模型.
/// </summary>
public sealed class MediaIdentifier(
    string id,
    string title,
    AppImage? cover = null,
    string? url = null
)
{
    /// <summary>
    /// 唯一标识.
    /// </summary>
    public string Id { get; init; } = id;

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; init; } = title;

    /// <summary>
    /// 封面.
    /// </summary>
    public AppImage? Cover { get; init; } = cover;

    /// <summary>
    /// 网页链接或跳转地址.
    /// </summary>
    public string? Url { get; init; } = url;
}
