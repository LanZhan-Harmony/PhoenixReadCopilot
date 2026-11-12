using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.Core.Models.User;

namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 视频/短剧收藏夹模型.
/// </summary>
public sealed class VideoFavoriteFolder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoFavoriteFolder"/> class.
    /// </summary>
    public VideoFavoriteFolder(
        string id,
        string title,
        AppImage? cover = null,
        User.User? user = null,
        string? description = null,
        int count = 0
    )
    {
        Id = id;
        Title = title;
        Cover = cover;
        User = user;
        Description = description;
        Count = count;
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
    /// 创建者.
    /// </summary>
    public User.User? User { get; init; }

    /// <summary>
    /// 说明.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 条目总数.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// 条目总数 (兼容别名).
    /// </summary>
    public int TotalCount => Count;
}
