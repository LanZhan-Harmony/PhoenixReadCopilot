using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

/// <summary>
/// 视频排序方式可读文本转换器.
/// </summary>
internal sealed partial class SortTypeTextConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string result = string.Empty;
        if (value is ComprehensiveSearchSortType chst)
        {
            result = chst switch
            {
                ComprehensiveSearchSortType.Default => Resources.SortByDefault,
                ComprehensiveSearchSortType.Danmaku => Resources.SortByDanmaku,
                ComprehensiveSearchSortType.Play => Resources.SortByPlay,
                ComprehensiveSearchSortType.Newest => Resources.SortByNewest,
                _ => string.Empty,
            };
        }
        else if (value is PhoenixReadApi.CommentSortOrder cso)
        {
            result = cso switch
            {
                PhoenixReadApi.CommentSortOrder.Hot => Resources.HotReply,
                PhoenixReadApi.CommentSortOrder.Latest => Resources.LatestReply,
                _ => string.Empty,
            };
        }

        return result;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
