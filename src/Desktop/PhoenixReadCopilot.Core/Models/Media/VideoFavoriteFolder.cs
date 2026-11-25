using PhoenixReadCopilot.Core.Models.Appearance;

namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 视频/短剧收藏夹模型.
/// </summary>
public sealed class VideoFavoriteFolder(
    string id,
    string title,
    AppImage? cover = null,
    User.User? user = null,
    string? description = null,
    int count = 0
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
    /// 创建者.
    /// </summary>
    public User.User? User { get; init; } = user;

    /// <summary>
    /// 说明.
    /// </summary>
    public string? Description { get; init; } = description;

    /// <summary>
    /// 条目总数.
    /// </summary>
    public int Count { get; init; } = count;

    /// <summary>
    /// 条目总数 (兼容别名).
    /// </summary>
    public int TotalCount => Count;
}
