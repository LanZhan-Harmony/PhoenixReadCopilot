using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 弹幕位置转换为可读文本.
    /// </summary>
    public static string DanmakuLocationToText(DanmakuLocation location) => location switch
    {
        DanmakuLocation.Scroll => Resources.ScrollDanmaku,
        DanmakuLocation.Top => Resources.TopDanmaku,
        DanmakuLocation.Bottom => Resources.BottomDanmaku,
        _ => string.Empty,
    };
}
