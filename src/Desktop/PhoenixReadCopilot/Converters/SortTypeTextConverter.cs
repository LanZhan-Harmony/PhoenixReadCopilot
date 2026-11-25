using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 综合搜索排序方式转换为可读文本.
    /// </summary>
    public static string SearchSortTypeToText(ComprehensiveSearchSortType sortType) => sortType switch
    {
        ComprehensiveSearchSortType.Default => Resources.SortByDefault,
        ComprehensiveSearchSortType.Danmaku => Resources.SortByDanmaku,
        ComprehensiveSearchSortType.Play => Resources.SortByPlay,
        ComprehensiveSearchSortType.Newest => Resources.SortByNewest,
        _ => string.Empty,
    };

    /// <summary>
    /// 评论排序方式转换为可读文本.
    /// </summary>
    public static string CommentSortOrderToText(CommentSortOrder sortOrder) => sortOrder switch
    {
        CommentSortOrder.Hot => Resources.HotReply,
        CommentSortOrder.Latest => Resources.LatestReply,
        _ => string.Empty,
    };
}
