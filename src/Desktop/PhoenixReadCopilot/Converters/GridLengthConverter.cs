using Microsoft.UI.Xaml;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 数值转换为 GridLength（小于等于 0 时返回 Auto）.
    /// </summary>
    public static GridLength DoubleToGridLength(double value) =>
        value <= 0 ? GridLength.Auto : new GridLength(value);
}
