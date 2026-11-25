using Windows.UI;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 将颜色转换为文本.
    /// </summary>
    public static string ColorToText(Color color) => color.ToString();
}
