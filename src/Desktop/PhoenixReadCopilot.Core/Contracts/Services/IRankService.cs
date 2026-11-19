using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 榜单服务接口，提供短剧排行榜与演职员排行榜查询.
/// </summary>
public interface IRankService
{
    /// <summary>
    /// 获取短剧排行榜的一页数据.
    /// </summary>
    /// <param name="type">排行榜类型（热播榜、新剧榜等）.</param>
    /// <param name="channel">频道类别（全部分类、男频、女频等）.</param>
    /// <param name="offset">分页偏移量，首次请求传 0，后续传上一页的 NextOffset.</param>
    /// <param name="sessionId">跨页会话 ID，首次请求可传 null，后续传上一页返回的值.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>短剧排行榜分页结果.</returns>
    Task<ShortDramaLeaderboardPage> GetShortDramaLeaderboardAsync(
        ShortDramaLeaderboardType type,
        ShortDramaLeaderboardChannel channel = ShortDramaLeaderboardChannel.All,
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取演职员排行榜的一页数据.
    /// </summary>
    /// <param name="offset">分页偏移量，首次请求传 0，后续传上一页的 NextOffset.</param>
    /// <param name="sessionId">跨页会话 ID，首次请求可传 null，后续传上一页返回的值.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>演职员排行榜分页结果.</returns>
    Task<ActorLeaderboardPage> GetActorLeaderboardAsync(
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    );
}
