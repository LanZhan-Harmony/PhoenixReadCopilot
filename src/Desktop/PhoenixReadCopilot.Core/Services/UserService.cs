using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 用户空间业务服务实现.
/// </summary>
public sealed class UserService(PhoenixReadApiClient apiClient, ILogger<UserService> logger)
    : IUserService
{
    private readonly PhoenixReadApiClient _apiClient = apiClient;
    private readonly ILogger<UserService> _logger = logger;

    /// <inheritdoc/>
    public async Task<UserProfile> GetUserProfileAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetUserProfileAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户主页资料失败: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<UserProfile> GetUserProfileAsync(
        DramaCredit credit,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetUserProfileAsync(credit, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据署名获取用户主页资料失败: {UserId}", credit.User?.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<UserWorkPage> GetUserWorksAsync(
        string userId,
        UserWorksQuery query,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetUserWorksAsync(userId, query, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "获取用户作品列表失败: {UserId}, SubTab={SubTab}",
                userId,
                query.SubTab
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DramaPage> GetUserFavoritesAsync(
        string userId,
        int count = 18,
        int offset = 0,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetUserFavoritesAsync(userId, count, offset, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户收藏失败: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<UserDramaPage> GetUserLikesAsync(
        string userId,
        int count = 18,
        int offset = 0,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetUserLikesAsync(userId, count, offset, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户点赞失败: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CirclePage> GetUserCircleAsync(
        string userId,
        int count = 18,
        string cursor = "",
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetUserCircleAsync(userId, count, cursor, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户红果圈失败: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<UserRelationPage> GetUserFollowingsAsync(
        string userId,
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetUserFollowingsAsync(
                userId,
                cursor,
                count,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户关注列表失败: {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<UserRelationPage> GetUserFollowersAsync(
        string userId,
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetUserFollowersAsync(userId, cursor, count, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户粉丝列表失败: {UserId}", userId);
            throw;
        }
    }
}
