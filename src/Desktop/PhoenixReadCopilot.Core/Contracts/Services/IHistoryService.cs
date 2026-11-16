using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 播放历史记录服务接口.
/// </summary>
public interface IHistoryService
{
    /// <summary>
    /// 历史记录变更事件（上报、删除、清空或切换登录状态时触发）.
    /// </summary>
    event EventHandler? HistoryChanged;

    /// <summary>
    /// 上报播放进度（同时发送到服务端并在本地持久化保存）.
    /// </summary>
    Task ReportProgressAsync(
        ProgressUpdate update,
        string? title = null,
        string? coverUrl = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 上报播放进度重载方法.
    /// </summary>
    Task ReportProgressAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        long currentPositionMs,
        long durationMs,
        string? title = null,
        string? coverUrl = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 分页获取播放历史.
    /// </summary>
    Task<ReadHistoryPage> GetHistoryAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 删除指定历史记录.
    /// </summary>
    Task DeleteHistoryAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 清空播放历史.
    /// </summary>
    Task ClearHistoryAsync(CancellationToken cancellationToken = default);
}
