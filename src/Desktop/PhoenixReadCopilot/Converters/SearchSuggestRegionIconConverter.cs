using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 搜索建议区域标识转换为图标.
    /// </summary>
    public static Symbol SearchSuggestRegionToSymbol(string? id) => id switch
    {
        "space" => Symbol.Person,
        "history" => Symbol.History,
        _ => Symbol.Search,
    };
}
