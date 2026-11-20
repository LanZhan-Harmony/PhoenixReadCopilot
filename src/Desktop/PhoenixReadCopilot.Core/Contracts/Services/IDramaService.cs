using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 短剧业务服务接口，提供短剧详情、分集列表、演职员信息、合集多季、分集点赞交互与分享短链生成.
/// </summary>
public interface IDramaService
{
    /// <summary>
    /// 获取单部短剧的完整详情及分集列表.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="forceRefresh">是否强制从服务端刷新，忽略本地缓存.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>包含短剧元数据和按集数排序分集列表的 <see cref="DramaDetails"/> 对象.</returns>
    Task<DramaDetails> GetDramaDetailsAsync(
        string dramaId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 批量获取多部短剧的基础元数据信息.
    /// </summary>
    /// <param name="dramaIds">短剧 ID 集合（单次上限建议 50 部）.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>成功匹配的短剧元数据列表.</returns>
    Task<IReadOnlyList<Drama>> GetBatchDramaDetailsAsync(
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 批量获取多部短剧的完整详情及分集列表.
    /// </summary>
    /// <param name="dramaIds">短剧 ID 集合（单次上限建议 50 部）.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>包含完整分集列表的 <see cref="DramaDetails"/> 集合.</returns>
    Task<IReadOnlyList<DramaDetails>> GetBatchDramaDetailsWithEpisodesAsync(
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 根据多季合集 ID 获取合集详情及各季列表.
    /// </summary>
    /// <param name="albumId">合集 ID.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>包含各季元数据的 <see cref="DramaAlbum"/> 对象.</returns>
    Task<DramaAlbum> GetAlbumAsync(
        string albumId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 聚合当前短剧的所有季数（多源回退与多季聚合）.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>按季数升序排列的完整季集合.</returns>
    Task<DramaAlbum> GetDramaSeasonsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取指定短剧的专属关联推荐好剧（播放器/详情页相关推荐）.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>包含推荐列表和推荐标题的 <see cref="DramaRecommendations"/> 对象.</returns>
    Task<DramaRecommendations> GetDramaRecommendationsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 点赞或取消点赞指定短剧的分集.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">分集视频 ID.</param>
    /// <param name="active">true 点赞，false 取消点赞.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SetDramaEpisodeLikeAsync(
        string dramaId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 为短剧或指定分集创建分享短链.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">分集视频 ID（可选）.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>包含长链和短链的分享对象 <see cref="ShareLink"/>.</returns>
    Task<ShareLink> CreateShareLinkAsync(
        string dramaId,
        string? videoId = null,
        CancellationToken cancellationToken = default
    );
}
