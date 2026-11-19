using PhoenixReadApi;
using CoreHotSearchItem = PhoenixReadCopilot.Core.Models.Search.HotSearchItem;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 搜索服务契约.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// 搜索历史记录发生变更时触发.
    /// </summary>
    event EventHandler? SearchHistoryChanged;

    /// <summary>
    /// 搜索短剧.
    /// </summary>
    /// <param name="keyword">搜索关键词.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>包含短剧列表及分页信息的搜索结果.</returns>
    Task<SearchResult> SearchAsync(string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用高级查询参数（游标、条数、分类等）搜索短剧或用户.
    /// </summary>
    /// <param name="query">高级搜索查询参数.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>搜索结果.</returns>
    Task<SearchResult> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取搜索建议/联想词.
    /// </summary>
    /// <param name="keyword">要联想的关键词.</param>
    /// <param name="maxResults">最多返回的建议数量.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>去重后的搜索建议列表.</returns>
    Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string keyword,
        int maxResults = 8,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取热搜榜单与热门推荐短剧（已映射为 UI 热搜模型）.
    /// </summary>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>热搜条目列表.</returns>
    Task<IReadOnlyList<CoreHotSearchItem>> GetHotSearchesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 获取原始完整热搜数据（包含所有榜单与猜你想搜）.
    /// </summary>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>原始热搜对象.</returns>
    Task<HotSearch> GetRawHotSearchAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取本地保存的搜索历史记录（按时间倒序排列）.
    /// </summary>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>搜索历史关键词列表.</returns>
    Task<IReadOnlyList<string>> GetSearchHistoryAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 添加一条搜索历史记录（若已存在则移至最前）.
    /// </summary>
    /// <param name="keyword">搜索关键词.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task AddSearchHistoryAsync(string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除指定的单条搜索历史记录.
    /// </summary>
    /// <param name="keyword">要移除的搜索关键词.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task RemoveSearchHistoryAsync(string keyword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清空所有搜索历史记录.
    /// </summary>
    /// <param name="cancellationToken">取消令牌.</param>
    Task ClearSearchHistoryAsync(CancellationToken cancellationToken = default);
}
