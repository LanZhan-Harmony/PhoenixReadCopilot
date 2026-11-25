using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    [ThreadStatic]
    private static Dictionary<Color, SolidColorBrush>? _brushCache;

    /// <summary>
    /// 将颜色转换为画刷（按 UI 线程缓存复用）.
    /// </summary>
    public static SolidColorBrush ColorToBrush(Color color)
    {
        _brushCache ??= [];
        if (!_brushCache.TryGetValue(color, out SolidColorBrush? brush))
        {
            brush = new SolidColorBrush(color);
            _brushCache[color] = brush;
        }

        return brush;
    }
}
