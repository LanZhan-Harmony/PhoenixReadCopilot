using PhoenixReadCopilot.Core.Models.Pin;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 固定条目管理服务接口.
/// </summary>
public interface IPinService
{
    /// <summary>
    /// 固定项变更事件.
    /// </summary>
    event EventHandler? PinChanged;

    /// <summary>
    /// 获取当前用户或匿名的所有固定项.
    /// </summary>
    Task<IReadOnlyList<PinItem>> GetPinnedItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查指定 ID 是否已固定（基于内存缓存）.
    /// </summary>
    bool IsPinned(string id);

    /// <summary>
    /// 异步检查指定 ID 是否已固定.
    /// </summary>
    Task<bool> IsPinnedAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加固定项.
    /// </summary>
    Task<bool> PinAsync(PinItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除固定项.
    /// </summary>
    Task<bool> UnpinAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除固定项.
    /// </summary>
    Task<bool> UnpinAsync(PinItem item, CancellationToken cancellationToken = default);
}
