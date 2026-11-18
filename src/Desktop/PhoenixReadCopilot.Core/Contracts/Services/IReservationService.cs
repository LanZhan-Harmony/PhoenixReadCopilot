using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 剧集预约服务接口（未登录存本地，登录与未登录分开存储）.
/// </summary>
public interface IReservationService
{
    /// <summary>
    /// 预约状态变更事件.
    /// </summary>
    event EventHandler? ReservationChanged;

    /// <summary>
    /// 检查指定剧集是否已预约（内存即时查询）.
    /// </summary>
    bool IsReserved(string dramaId);

    /// <summary>
    /// 异步检查指定剧集是否已预约.
    /// </summary>
    Task<bool> IsReservedAsync(string dramaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户或匿名的预约列表（支持分页）.
    /// </summary>
    Task<IReadOnlyList<ReservationItem>> GetReservationsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 设置剧集预约状态.
    /// 未登录保存至本地文件，已登录调用 API 并在本地建立缓存.
    /// </summary>
    Task<bool> SetReservationAsync(
        string dramaId,
        bool isReserved,
        string? title = null,
        string? coverUrl = null,
        string? videoId = null,
        long releaseAtUnixSeconds = 0,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 刷新预约列表（重新从服务器或本地同步缓存）.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
