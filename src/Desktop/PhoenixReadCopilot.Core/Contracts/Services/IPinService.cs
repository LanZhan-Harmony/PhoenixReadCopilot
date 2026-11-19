using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Pin;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 固定条目管理服务接口（支持短剧、用户，并预留视频、图文、书籍等扩展，支持登录与未登录隔离分文件存储）.
/// </summary>
public interface IPinService
{
    /// <summary>
    /// 固定项变更事件.
    /// </summary>
    event EventHandler? PinChanged;

    /// <summary>
    /// 获取当前用户或匿名的所有固定项（可按内容类型筛选）.
    /// </summary>
    Task<IReadOnlyList<PinItem>> GetPinnedItemsAsync(
        PinType? type = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 检查指定 ID 是否已固定（基于内存秒级响应）.
    /// </summary>
    bool IsPinned(string id, PinType? type = null);

    /// <summary>
    /// 异步检查指定 ID 是否已固定.
    /// </summary>
    Task<bool> IsPinnedAsync(
        string id,
        PinType? type = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 添加固定项.
    /// </summary>
    Task<bool> PinAsync(PinItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除固定项.
    /// </summary>
    Task<bool> UnpinAsync(
        string id,
        PinType? type = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 移除固定项.
    /// </summary>
    Task<bool> UnpinAsync(PinItem item, CancellationToken cancellationToken = default);
}
