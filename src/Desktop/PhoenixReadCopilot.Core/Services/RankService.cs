using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 榜单服务实现，提供短剧排行榜与演职员排行榜拉取.
/// </summary>
public sealed class RankService : IRankService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<RankService> _logger;

    public RankService(PhoenixReadApiClient apiClient, ILogger<RankService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ShortDramaLeaderboardPage> GetShortDramaLeaderboardAsync(
        ShortDramaLeaderboardType type,
        ShortDramaLeaderboardChannel channel = ShortDramaLeaderboardChannel.All,
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetShortDramaLeaderboardAsync(
                type: type,
                channel: channel,
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
            _logger.ZLogError(
                ex,
                $"获取短剧排行榜失败: type={type}, channel={channel}, offset={offset}"
            );
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ActorLeaderboardPage> GetActorLeaderboardAsync(
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetActorLeaderboardAsync(
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
            _logger.ZLogError(ex, $"获取演职员排行榜失败: offset={offset}");
            throw;
        }
    }
}
