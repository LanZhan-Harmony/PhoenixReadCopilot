using PhoenixReadCopilot.Core.Enums;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 固定项类型转换为图标.
    /// </summary>
    public static Symbol PinTypeToSymbol(PinType type) => type switch
    {
        PinType.User => Symbol.Person,
        PinType.Drama => Symbol.VideoClip,
        PinType.Video => Symbol.Video,
        PinType.Post => Symbol.Document,
        PinType.Novel => Symbol.Book,
        _ => Symbol.DocumentBulletList,
    };
}
