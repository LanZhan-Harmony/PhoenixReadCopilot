using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 搜索分区类型转换为可读文本.
    /// </summary>
    public static string SearchSectionToText(SearchSectionType sectionType) => sectionType switch
    {
        SearchSectionType.Drama => Resources.Drama,
        SearchSectionType.User => Resources.User,
        _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
    };
}
