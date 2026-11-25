using PhoenixReadCopilot.Helpers;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 整数计数格式化为可读文本.
    /// </summary>
    public static string CountToText(long? count) => FormatHelper.FormatCount(count ?? 0);

    /// <summary>
    /// 浮点计数格式化为可读文本.
    /// </summary>
    public static string DoubleCountToText(double count) => FormatHelper.FormatCount((long)count);
}
