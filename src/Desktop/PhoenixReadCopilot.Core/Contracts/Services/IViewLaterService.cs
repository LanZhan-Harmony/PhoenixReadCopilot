using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 稍后再看服务接口（支持短剧，并预留视频、图文、书籍等扩展，支持登录与未登录隔离分文件存储）.
/// </summary>
public interface IViewLaterService
{
    /// <summary>
    /// 稍后再看列表变更事件（添加、移除、清空或切换登录状态时触发）.
    /// </summary>
    event EventHandler? ViewLaterChanged;

    /// <summary>
    /// 检查指定内容是否已加入稍后再看（基于内存秒级响应）.
    /// </summary>
    bool IsInViewLater(string id, ViewLaterType type = ViewLaterType.Drama);

    /// <summary>
    /// 异步检查指定内容是否已加入稍后再看.
    /// </summary>
    Task<bool> IsInViewLaterAsync(
        string id,
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 添加短剧到稍后再看.
    /// </summary>
    Task AddAsync(
        Drama drama,
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 从稍后再看中移除短剧.
    /// </summary>
    Task RemoveAsync(
        string id,
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 清空稍后再看列表.
    /// </summary>
    Task ClearAsync(
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取全部稍后再看内容.
    /// </summary>
    Task<IReadOnlyList<Drama>> GetAllAsync(
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    );
}
