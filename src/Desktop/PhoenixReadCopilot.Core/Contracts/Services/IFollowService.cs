using PhoenixReadApi;
using User = PhoenixReadApi.User;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 关注与粉丝服务契约，负责创作者与演职员关注状态维护、关注/粉丝列表获取（粉丝仅在登录时支持）.
/// </summary>
public interface IFollowService
{
    /// <summary>
    /// 关注列表或关注状态发生变更时触发.
    /// </summary>
    event EventHandler? FollowChanged;

    /// <summary>
    /// 同步快速检查指定账号是否已关注（基于内存状态）.
    /// </summary>
    /// <param name="userId">用户账号 ID.</param>
    /// <returns>是否已关注.</returns>
    bool IsFollowing(string userId);

    /// <summary>
    /// 异步检查指定账号是否已关注（若内存未初始化则从本地或云端获取）.
    /// </summary>
    /// <param name="userId">用户账号 ID.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>是否已关注.</returns>
    Task<bool> IsFollowingAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 关注或取消关注一个账号（已登录时双写云端与本地缓存，未登录时保存至本地离线关注列表）.
    /// </summary>
    /// <param name="userId">用户账号 ID.</param>
    /// <param name="active">true 表示关注，false 表示取消关注.</param>
    /// <param name="user">可选的用户基本信息（用于离线关注时记录昵称和头像）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>操作结果.</returns>
    Task<PhoenixOperationResult> SetFollowAsync(
        string userId,
        bool active = true,
        User? user = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 切换指定账号的关注状态.
    /// </summary>
    /// <param name="userId">用户账号 ID.</param>
    /// <param name="user">可选的用户基本信息.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>切换后的关注状态（true 为关注，false 为未关注）.</returns>
    Task<bool> ToggleFollowAsync(
        string userId,
        User? user = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取关注账号列表（已登录时拉取云端，未登录时拉取本地离线关注列表）.
    /// </summary>
    /// <param name="userId">目标账号 ID；传 null 或空时默认为当前账号.</param>
    /// <param name="cursor">分页游标.</param>
    /// <param name="count">返回条数（默认 20）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>关注人员列表分页数据.</returns>
    Task<UserRelationPage> GetFollowingsAsync(
        string? userId = null,
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取粉丝列表（仅在已登录时支持；未登录时返回空列表）.
    /// </summary>
    /// <param name="userId">目标账号 ID；传 null 或空时默认为当前账号.</param>
    /// <param name="cursor">分页游标.</param>
    /// <param name="count">返回条数（默认 20）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>粉丝列表分页数据；未登录时 Items 为空.</returns>
    Task<UserRelationPage> GetFollowersAsync(
        string? userId = null,
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    );
}
