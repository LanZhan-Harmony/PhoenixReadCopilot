using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 分类筛选服务接口，提供多维筛选面板与组合检索.
/// </summary>
public interface IFilterService
{
    /// <summary>
    /// 获取多维筛选面板矩阵（包含各维度与筛选项）.
    /// </summary>
    /// <param name="genre">短剧体裁（默认微短剧 <see cref="ShortDramaGenre.ShortPlay"/>）.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>筛选维度组列表.</returns>
    Task<IReadOnlyList<ShortDramaFilterGroup>> GetFilterPanelAsync(
        ShortDramaGenre genre = ShortDramaGenre.ShortPlay,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 按多维筛选条件组合过滤短剧.
    /// </summary>
    /// <param name="query">筛选请求对象，包含所选维度、游标与分页配置.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>筛选结果分页.</returns>
    Task<ShortDramaFilterPage> FilterShortDramasAsync(
        ShortDramaFilterQuery query,
        CancellationToken cancellationToken = default
    );
}
