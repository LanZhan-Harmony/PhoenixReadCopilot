using PhoenixReadCopilot.Helpers;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 秒数格式化为时长文本.
    /// </summary>
    public static string DurationToText(int seconds) =>
        FormatHelper.FormatDuration(TimeSpan.FromSeconds(seconds));
}
