using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Models.Like;

/// <summary>
/// 短剧点赞数据项，记录整部剧信息以及该剧下具体已点赞的分集视频 ID 列表.
/// </summary>
public sealed class DramaLikeItem
{
    /// <summary>
    /// 短剧唯一标识.
    /// </summary>
    public string DramaId { get; set; } = string.Empty;

    /// <summary>
    /// 短剧基本信息（标题、封面、总集数等）.
    /// </summary>
    public Drama Drama { get; set; } = new();

    /// <summary>
    /// 该短剧中已点赞的分集视频 ID 列表（每一集独立记录点赞状态）.
    /// </summary>
    public List<string> LikedVideoIds { get; set; } = [];

    /// <summary>
    /// 最近点赞/更新时间戳 (Unix 秒).
    /// </summary>
    public long UpdatedAtUnixSeconds { get; set; }
}
