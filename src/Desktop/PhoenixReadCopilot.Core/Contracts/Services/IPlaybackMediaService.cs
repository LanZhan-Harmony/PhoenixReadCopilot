using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 播放媒体源服务接口（统一管理媒体地址解析、持久化缓存与失效刷新）.
/// </summary>
public interface IPlaybackMediaService
{
    /// <summary>
    /// 解析并获取分集的播放媒体列表（按清晰度从高到低排序）.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="forceRefresh">是否强制刷新（忽略本地缓存，重新向服务端请求并覆盖缓存）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>播放媒体列表.</returns>
    Task<IReadOnlyList<PlaybackMedia>> ResolveMediaAsync(
        string dramaId,
        string videoId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 使指定分集或整部剧的本地媒体缓存失效.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">分集视频 ID；若为 null 则清空整部剧的所有缓存.</param>
    Task InvalidateCacheAsync(string dramaId, string? videoId = null);

    /// <summary>
    /// 检查指定分集是否存在有效的本地缓存.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <returns>若存在未过期的有效缓存则返回 true.</returns>
    Task<bool> HasValidCacheAsync(string dramaId, string videoId);
}
