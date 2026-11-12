using PhoenixReadCopilot.Core.Models.Appearance;

namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 媒体基础标识模型.
/// </summary>
public sealed class MediaIdentifier
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaIdentifier"/> class.
    /// </summary>
    public MediaIdentifier(string id, string title, AppImage? cover = null, string? url = null)
    {
        Id = id;
        Title = title;
        Cover = cover;
        Url = url;
    }

    /// <summary>
    /// 唯一标识.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 封面.
    /// </summary>
    public AppImage? Cover { get; init; }

    /// <summary>
    /// 网页链接或跳转地址.
    /// </summary>
    public string? Url { get; init; }
}
