using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 短剧业务服务实现，负责短剧详情、分集列表、合集聚合、分集点赞与分享短链生成.
/// </summary>
public sealed class DramaService : IDramaService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<DramaService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, (DramaDetails Details, DateTime ExpireAt)> _detailsCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="DramaService"/> class.
    /// </summary>
    public DramaService(
        PhoenixReadApiClient apiClient,
        IAuthenticationService authService,
        ILogger<DramaService> logger
    )
    {
        _apiClient = apiClient;
        _authService = authService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DramaDetails> GetDramaDetailsAsync(
        string dramaId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        if (!forceRefresh)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                if (
                    _detailsCache.TryGetValue(
                        dramaId,
                        out (DramaDetails Details, DateTime ExpireAt) entry
                    )
                    && entry.ExpireAt > DateTime.UtcNow
                )
                {
                    return entry.Details;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        try
        {
            DramaDetails details = await _apiClient.GetDramaDetailsAsync(dramaId, cancellationToken);

            await _lock.WaitAsync(cancellationToken);
            try
            {
                _detailsCache[dramaId] = (details, DateTime.UtcNow.Add(CacheDuration));
            }
            finally
            {
                _lock.Release();
            }

            return details;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取短剧详情失败: dramaId={dramaId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Drama>> GetBatchDramaDetailsAsync(
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(dramaIds);

        try
        {
            return await _apiClient.GetBatchDramaDetailsAsync(dramaIds, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"批量获取短剧基础信息失败.");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DramaDetails>> GetBatchDramaDetailsWithEpisodesAsync(
        IEnumerable<string> dramaIds,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(dramaIds);

        try
        {
            return await _apiClient.GetBatchDramaDetailsWithEpisodesAsync(
                dramaIds,
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"批量获取短剧完整分集详情失败.");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DramaAlbum> GetAlbumAsync(
        string albumId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumId);

        try
        {
            return await _apiClient.GetAlbumAsync(albumId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取短剧合集信息失败: albumId={albumId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DramaAlbum> GetDramaSeasonsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        try
        {
            return await _apiClient.GetDramaSeasonsAsync(dramaId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"聚合短剧季集合失败: dramaId={dramaId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DramaRecommendations> GetDramaRecommendationsAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        try
        {
            return await _apiClient.GetDramaRecommendationsAsync(dramaId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取短剧专属推荐好剧失败: dramaId={dramaId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SetDramaEpisodeLikeAsync(
        string dramaId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        if (!_authService.IsAuthenticated)
        {
            return new PhoenixOperationResult { Succeeded = false, Message = "请先登录" };
        }

        try
        {
            return await _apiClient.SetDramaEpisodeLikeAsync(
                dramaId,
                videoId,
                active,
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"设置短剧分集点赞失败: dramaId={dramaId}, videoId={videoId}, active={active}"
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ShareLink> CreateShareLinkAsync(
        string dramaId,
        string? videoId = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        try
        {
            return await _apiClient.CreateShareLinkAsync(dramaId, videoId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"创建短剧分享短链失败: dramaId={dramaId}, videoId={videoId}");
            throw;
        }
    }
}
