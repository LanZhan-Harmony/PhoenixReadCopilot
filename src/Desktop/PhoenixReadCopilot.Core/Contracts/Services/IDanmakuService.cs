using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 弹幕服务契约，负责分集弹幕拉取、本地持久化缓存、弹幕发送与互动操作.
/// </summary>
public interface IDanmakuService
{
    /// <summary>
    /// 获取指定分集在指定时间窗口内的最新弹幕.
    /// </summary>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="startMs">时间窗口起点（毫秒）.</param>
    /// <param name="durationMs">时间窗口长度（毫秒）.</param>
    /// <param name="forceRefresh">是否强制从服务端刷新，忽略本地缓存.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>合并、去重并按时间排序后的弹幕页.</returns>
    Task<DanmakuPage> GetDanmakuAsync(
        string dramaId,
        string videoId,
        long startMs = 0,
        long durationMs = 3600000,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 发送一条弹幕（需要登录）.
    /// </summary>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="text">弹幕文本内容.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>服务端操作结果.</returns>
    Task<PhoenixOperationResult> SendDanmakuAsync(
        string dramaId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 点赞或取消点赞弹幕.
    /// </summary>
    /// <param name="commentId">弹幕评论 ID.</param>
    /// <param name="videoId">视频 ID.</param>
    /// <param name="active">true 表示点赞，false 表示取消点赞.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>服务端操作结果.</returns>
    Task<PhoenixOperationResult> SetDanmakuLikeAsync(
        string commentId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 删除一条自己发送的弹幕.
    /// </summary>
    /// <param name="commentId">弹幕评论 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>服务端操作结果.</returns>
    Task<PhoenixOperationResult> DeleteDanmakuAsync(
        string commentId,
        string videoId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 清理指定短剧或特定分集的本地弹幕缓存.
    /// </summary>
    /// <param name="dramaId">剧集 ID.</param>
    /// <param name="videoId">分集视频 ID（为空时清理整部剧的所有分集缓存）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task ClearDanmakuCacheAsync(
        string dramaId,
        string? videoId = null,
        CancellationToken cancellationToken = default
    );
}
