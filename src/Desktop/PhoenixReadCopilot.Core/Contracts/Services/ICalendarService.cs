using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 上新日历服务接口，提供短剧排期、按日上新时间线与剧集查询.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// 获取短剧上新日历的一页数据.
    /// </summary>
    /// <param name="targetDate">目标日期；传 null 时由服务端返回默认日期（通常为当天）.</param>
    /// <param name="offset">分页偏移量，首次请求传 0，后续传上一页返回的 NextOffset.</param>
    /// <param name="sessionId">跨页会话 ID，首次请求可传 null，后续传上一页返回的值.</param>
    /// <param name="cancellationToken">取消请求的令牌.</param>
    /// <returns>包含可用日期列表、当前日期短剧和分页状态的 <see cref="DailyReleasePage"/>.</returns>
    Task<DailyReleasePage> GetDailyReleasesAsync(
        DateOnly? targetDate = null,
        int offset = 0,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    );
}
