using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Strings;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.Converters;

/// <summary>
/// 视频排序方式可读文本转换器.
/// </summary>
internal sealed partial class SortTypeTextConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var result = string.Empty;
        if (value is PartitionVideoSortType vst)
        {
            switch (vst)
            {
                case PartitionVideoSortType.Default:
                    result = Resources.SortByDefault;
                    break;
                case PartitionVideoSortType.Newest:
                    result = Resources.SortByNewest;
                    break;
                case PartitionVideoSortType.Play:
                    result = Resources.SortByPlay;
                    break;
                case PartitionVideoSortType.Reply:
                    result = Resources.SortByReply;
                    break;
                case PartitionVideoSortType.Danmaku:
                    result = Resources.SortByDanmaku;
                    break;
                case PartitionVideoSortType.Favorite:
                    result = Resources.SortByFavorite;
                    break;
                default:
                    break;
            }
        }
        else if (value is ArticleSortType ast)
        {
            switch (ast)
            {
                case ArticleSortType.Default:
                    result = Resources.SortByDefault;
                    break;
                case ArticleSortType.Newest:
                    result = Resources.SortByNewest;
                    break;
                case ArticleSortType.Read:
                    result = Resources.SortByRead;
                    break;
                case ArticleSortType.Reply:
                    result = Resources.SortByReply;
                    break;
                case ArticleSortType.Like:
                    result = Resources.SortByLike;
                    break;
                case ArticleSortType.Favorite:
                    result = Resources.SortByFavorite;
                    break;
                default:
                    break;
            }
        }
        else if (value is ComprehensiveSearchSortType chst)
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
        else if (value is CommentSortType cst)
        {
            result = cst switch
            {
                CommentSortType.Hot => Resources.HotReply,
                CommentSortType.Time => Resources.LatestReply,
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };
        }

        return result;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
