using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 点赞服务契约，负责短剧分集点赞、预留内容类型点赞状态维护与本地/云端持久化存储.
/// </summary>
public interface ILikeService
{
    /// <summary>
    /// 点赞状态发生变更事件.
    /// </summary>
    event EventHandler? LikeChanged;

    /// <summary>
    /// 同步检查指定短剧的特定单集是否已点赞（基于内存秒级响应）.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">单集视频 ID.</param>
    /// <returns>是否已点赞.</returns>
    bool IsLiked(string dramaId, string videoId);

    /// <summary>
    /// 同步检查指定内容是否已点赞（预留类型：单个视频、图文、小说）.
    /// </summary>
    /// <param name="id">内容 ID.</param>
    /// <param name="type">点赞内容类型.</param>
    /// <returns>是否已点赞.</returns>
    bool IsLiked(string id, LikeType type);

    /// <summary>
    /// 异步检查指定短剧的特定单集是否已点赞（确保本地/云端数据加载完毕）.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">单集视频 ID.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>是否已点赞.</returns>
    Task<bool> IsLikedAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 异步检查指定内容是否已点赞（预留类型）.
    /// </summary>
    /// <param name="id">内容 ID.</param>
    /// <param name="type">点赞内容类型.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>是否已点赞.</returns>
    Task<bool> IsLikedAsync(
        string id,
        LikeType type,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取指定短剧中所有已点赞的分集视频 ID 集合.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <returns>已点赞的分集视频 ID 集合.</returns>
    IReadOnlySet<string> GetLikedVideoIds(string dramaId);

    /// <summary>
    /// 点赞或取消点赞短剧中的指定单集（已登录双写云端与本地缓存，未登录离线持久化）.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="active">true 为点赞，false 为取消点赞.</param>
    /// <param name="drama">可选的短剧基本信息（用于本地保存展示）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SetDramaEpisodeLikeAsync(
        string dramaId,
        string videoId,
        bool active = true,
        Drama? drama = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 设置指定内容类型的点赞状态（预留单视频、图文、小说等扩展支持）.
    /// </summary>
    /// <param name="id">内容 ID.</param>
    /// <param name="active">true 为点赞，false 为取消点赞.</param>
    /// <param name="type">点赞内容类型.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SetLikeAsync(
        string id,
        bool active,
        LikeType type,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取当前用户（登录或未登录）已点赞的短剧列表（支持分页）.
    /// </summary>
    /// <param name="count">本页数量.</param>
    /// <param name="offset">分页偏移量.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>短剧分页数据.</returns>
    Task<DramaPage> GetLikesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 刷新点赞缓存（重新加载本地或同步服务器数据）.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
