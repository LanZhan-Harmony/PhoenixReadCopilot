using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 本地稍后再看服务接口.
/// </summary>
public interface IViewLaterService
{
    /// <summary>
    /// 稍后再看列表变更事件（添加、移除、清空或切换登录状态时触发）.
    /// </summary>
    event EventHandler? ViewLaterChanged;

    /// <summary>
    /// 添加短剧到稍后再看.
    /// </summary>
    Task AddAsync(Drama drama);

    /// <summary>
    /// 从稍后再看中移除短剧.
    /// </summary>
    Task RemoveAsync(string dramaId);

    /// <summary>
    /// 清空稍后再看列表.
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// 获取全部稍后再看短剧.
    /// </summary>
    Task<IReadOnlyList<Drama>> GetAllAsync();
}
