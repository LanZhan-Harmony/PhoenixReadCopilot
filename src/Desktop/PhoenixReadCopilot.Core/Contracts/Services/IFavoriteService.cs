using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 收藏/追剧服务接口（面向整部短剧，支持登录与未登录隔离存储，预留书籍与图文扩展）.
/// </summary>
public interface IFavoriteService
{
    /// <summary>
    /// 收藏状态变更事件.
    /// </summary>
    event EventHandler? FavoriteChanged;

    /// <summary>
    /// 检查指定短剧是否已收藏（整部剧即时查询，内存秒级响应）.
    /// </summary>
    bool IsFavorited(string dramaId, FavoriteType type = FavoriteType.Drama);

    /// <summary>
    /// 异步检查指定内容是否已收藏.
    /// </summary>
    Task<bool> IsFavoritedAsync(
        string id,
        FavoriteType type = FavoriteType.Drama,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取当前用户或匿名的短剧收藏/追剧列表（支持分页）.
    /// </summary>
    Task<DramaPage> GetFavoritesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 设置短剧收藏状态（收藏/取消收藏整部剧）.
    /// 未登录保存至本地文件，已登录调用 API 并在本地建立缓存.
    /// </summary>
    Task<bool> SetFavoriteAsync(
        string dramaId,
        bool isFavorite,
        Drama? drama = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 设置指定类型内容的收藏状态（预留图文、小说等扩展支持）.
    /// </summary>
    Task<bool> SetFavoriteAsync(
        string id,
        bool isFavorite,
        FavoriteType type,
        Drama? drama = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 刷新收藏列表（重新从服务器或本地同步缓存）.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
