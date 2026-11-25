using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 推荐服务接口，提供首页信息流推荐、新剧推荐与短剧关联推荐.
/// </summary>
public interface IRecommendService
{
    /// <summary>
    /// 获取首页指定频道的发现页内容列表及流式翻页游标.
    /// </summary>
    /// <param name="channel">发现频道枚举（默认为 <see cref="DiscoverChannel.Recommend"/>）.</param>
    /// <param name="cursor">分页游标，首次请求可传 null，后续传上一页返回的 Cursor.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>发现页分页结果.</returns>
    Task<DiscoverPage> GetDiscoverPageAsync(
        DiscoverChannel channel = DiscoverChannel.Recommend,
        string? cursor = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取个性化信息流推荐短剧的一页数据.
    /// </summary>
    /// <param name="genre">推荐题材分类枚举（默认为短剧 <see cref="RecommendationGenre.ShortDrama"/>）.</param>
    /// <param name="offset">分页偏移量，首次请求传 0，后续传上一页返回的 NextOffset.</param>
    /// <param name="sessionId">跨页会话 ID，首次请求可传 null，后续传上一页返回的值.</param>
    /// <param name="seen">已展示过的剧集 ID 集合，用于服务端协同去重.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>推荐短剧分页结果.</returns>
    Task<RecommendationPage> GetRecommendationsAsync(
        RecommendationGenre genre = RecommendationGenre.ShortDrama,
        int offset = 0,
        string? sessionId = null,
        IEnumerable<string>? seen = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取新剧专属推荐的一页数据.
    /// </summary>
    /// <param name="offset">分页偏移量，首次请求传 0，后续传上一页返回的 NextOffset.</param>
    /// <param name="sessionId">跨页会话 ID，首次请求可传 null，后续传上一页返回的值.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>新剧推荐分页结果.</returns>
    Task<NewDramaRecommendationPage> GetNewDramaRecommendationsAsync(
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取指定短剧的关联推荐好剧（播放器/详情页相关推荐）.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>包含推荐列表和推荐标题的 <see cref="DramaRecommendations"/> 对象.</returns>
    Task<DramaRecommendations> GetDramaRecommendationsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    );
}
