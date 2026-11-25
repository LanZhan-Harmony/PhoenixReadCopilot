using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 媒体传输控件行为转换为可读文本.
    /// </summary>
    public static string MTCBehaviorToText(MTCBehavior behavior) => behavior switch
    {
        MTCBehavior.Automatic => Resources.Automatic,
        MTCBehavior.Manual => Resources.Manual,
        _ => string.Empty,
    };
}
