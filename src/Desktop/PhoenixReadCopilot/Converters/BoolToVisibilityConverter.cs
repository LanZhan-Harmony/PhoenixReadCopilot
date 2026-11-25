using Microsoft.UI.Xaml;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 反转布尔值并转换为可见性（true -> Collapsed，false -> Visible）.
    /// </summary>
    public static Visibility InvertVisibility(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;
}
