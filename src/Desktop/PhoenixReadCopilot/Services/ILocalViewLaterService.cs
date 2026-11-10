using PhoenixReadApi;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 本地稍后再看服务接口.
/// </summary>
public interface ILocalViewLaterService
{
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
