using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 推荐服务实现，提供首页推荐流、新剧推荐与短剧关联推荐.
/// </summary>
public sealed class RecommendService : IRecommendService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<RecommendService> _logger;

    public RecommendService(PhoenixReadApiClient apiClient, ILogger<RecommendService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<RecommendationPage> GetRecommendationsAsync(
        RecommendationGenre genre = RecommendationGenre.ShortDrama,
        int offset = 0,
        string? sessionId = null,
        IEnumerable<string>? seen = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetRecommendationsAsync(
                genre: genre,
                offset: offset,
                sessionId: sessionId,
                seen: seen,
                cancellationToken: cancellationToken
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
                $"获取短剧推荐流失败: genre={genre}, offset={offset}"
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<NewDramaRecommendationPage> GetNewDramaRecommendationsAsync(
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetNewDramaRecommendationsAsync(
                offset: offset,
                sessionId: sessionId,
                cancellationToken: cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取新剧推荐失败: offset={offset}");
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
            _logger.ZLogError(ex, $"获取短剧关联推荐失败: dramaId={dramaId}");
            throw;
        }
    }
}
