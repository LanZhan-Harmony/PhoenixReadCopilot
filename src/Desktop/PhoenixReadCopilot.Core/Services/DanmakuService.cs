using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 弹幕服务实现，负责分集弹幕在线拉取、发送与互动操作（不缓存到本地，每次均重新获取最新弹幕）.
/// </summary>
public sealed class DanmakuService(PhoenixReadApiClient apiClient, ILogger<DanmakuService> logger)
    : IDanmakuService
{
    private readonly PhoenixReadApiClient _apiClient = apiClient;
    private readonly ILogger<DanmakuService> _logger = logger;

    /// <inheritdoc/>
    public async Task<DanmakuPage> GetDanmakuAsync(
        string dramaId,
        string videoId,
        long startMs = 0,
        long durationMs = 3600000,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        try
        {
            return await _apiClient.GetDanmakuAsync(
                dramaId,
                videoId,
                startMs,
                durationMs,
                cancellationToken
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"拉取分集弹幕失败: dramaId={dramaId}, videoId={videoId}");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<PhoenixOperationResult> SendDanmakuAsync(
        string dramaId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return _apiClient.SendDanmakuAsync(dramaId, videoId, text.Trim(), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<PhoenixOperationResult> SetDanmakuLikeAsync(
        string commentId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        return _apiClient.SetDanmakuLikeAsync(commentId, videoId, active, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<PhoenixOperationResult> DeleteDanmakuAsync(
        string commentId,
        string videoId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        return _apiClient.DeleteDanmakuAsync(commentId, videoId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task ClearDanmakuCacheAsync(
        string dramaId,
        string? videoId = null,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }
}
