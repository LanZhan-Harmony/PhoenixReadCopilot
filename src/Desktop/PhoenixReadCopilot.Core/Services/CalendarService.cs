using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 上新日历服务实现，提供按日排期查询与短剧发现.
/// </summary>
public sealed class CalendarService(PhoenixReadApiClient apiClient, ILogger<CalendarService> logger)
    : ICalendarService
{
    private readonly PhoenixReadApiClient _apiClient = apiClient;
    private readonly ILogger<CalendarService> _logger = logger;

    /// <inheritdoc/>
    public async Task<DailyReleasePage> GetDailyReleasesAsync(
        DateOnly? targetDate = null,
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _apiClient.GetDailyReleasesAsync(
                targetDate: targetDate,
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
                $"获取短剧上新日历失败: targetDate={targetDate}, offset={offset}"
            );
            throw;
        }
    }
}
