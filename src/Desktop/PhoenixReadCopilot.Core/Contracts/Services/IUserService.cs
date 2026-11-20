using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 用户空间业务服务接口，提供用户资料、作品列表、红果圈动态、收藏与点赞记录拉取.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取用户主页信息与首批公开内容.
    /// </summary>
    /// <param name="userId">用户 ID.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task<UserProfile> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据演职人员署名关系获取用户主页.
    /// </summary>
    /// <param name="credit">演职人员署名关系.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task<UserProfile> GetUserProfileAsync(DramaCredit credit, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户主页作品列表（支持全部、剧作、预约、合集、剧评等子分类及排序）.
    /// </summary>
    /// <param name="userId">用户 ID.</param>
    /// <param name="query">查询参数.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task<UserWorkPage> GetUserWorksAsync(string userId, UserWorksQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户公开收藏剧集.
    /// </summary>
    /// <param name="userId">用户 ID.</param>
    /// <param name="count">数量.</param>
    /// <param name="offset">偏移量.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task<DramaPage> GetUserFavoritesAsync(string userId, int count = 18, int offset = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户公开点赞剧集.
    /// </summary>
    /// <param name="userId">用户 ID.</param>
    /// <param name="count">数量.</param>
    /// <param name="offset">偏移量.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task<UserDramaPage> GetUserLikesAsync(string userId, int count = 18, int offset = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户红果圈动态列表（图文与视频）.
    /// </summary>
    /// <param name="userId">用户 ID.</param>
    /// <param name="count">数量.</param>
    /// <param name="cursor">分页游标.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task<CirclePage> GetUserCircleAsync(string userId, int count = 18, string cursor = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户关注列表.
    /// </summary>
    /// <param name="userId">用户 ID.</param>
    /// <param name="cursor">分页游标.</param>
    /// <param name="count">数量.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task<UserRelationPage> GetUserFollowingsAsync(string userId, string cursor = "", int count = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户粉丝列表.
    /// </summary>
    /// <param name="userId">用户 ID.</param>
    /// <param name="cursor">分页游标.</param>
    /// <param name="count">数量.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task<UserRelationPage> GetUserFollowersAsync(string userId, string cursor = "", int count = 20, CancellationToken cancellationToken = default);
}
