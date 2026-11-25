using PhoenixReadCopilot.Core.Enums;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 搜索分区类型转换为图标.
    /// </summary>
    public static Symbol SearchSectionToSymbol(SearchSectionType sectionType) => sectionType switch
    {
        SearchSectionType.Drama => Symbol.VideoClip,
        SearchSectionType.User => Symbol.People,
        _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
    };
}
