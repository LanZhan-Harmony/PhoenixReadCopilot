using System.Globalization;
using Windows.UI;

namespace PhoenixReadCopilot.Helpers;

internal static class ColorHelper
{
    /// <summary>
    /// 将颜色代码转换为Windows.UI.Color对象.
    /// </summary>
    /// <param name="hexCode">颜色代码.</param>
    /// <returns><see cref="Color"/>.</returns>
    public static Color HexToColor(string hexCode)
    {
        // 去除可能包含的 # 符号
        if (hexCode.StartsWith('#'))
        {
            hexCode = hexCode[1..];
        }

        if (int.TryParse(hexCode, out int intValue))
        {
            hexCode = intValue.ToString("X2");
        }

        Color color = default;
        if (hexCode.Length == 4)
        {
            hexCode = "00" + hexCode;
        }

        if (hexCode.Length == 6)
        {
            color.R = byte.Parse(hexCode[..2], NumberStyles.HexNumber);
            color.G = byte.Parse(hexCode.Substring(2, 2), NumberStyles.HexNumber);
            color.B = byte.Parse(hexCode.Substring(4, 2), NumberStyles.HexNumber);
            color.A = 255;
        }

        if (hexCode.Length == 8)
        {
            color.R = byte.Parse(hexCode.Substring(2, 2), NumberStyles.HexNumber);
            color.G = byte.Parse(hexCode.Substring(4, 2), NumberStyles.HexNumber);
            color.B = byte.Parse(hexCode.Substring(6, 2), NumberStyles.HexNumber);
            color.A = byte.Parse(hexCode[..2], NumberStyles.HexNumber);
        }

        return color;
    }

    /// <summary>
    /// 将 0xAARRGGBB 格式的无符号整数转换为 <see cref="Color"/>.
    /// </summary>
    /// <param name="argb">ARGB 颜色值.</param>
    /// <returns><see cref="Color"/>.</returns>
    public static Color FromArgbUInt(uint argb)
    {
        byte a = (byte)((argb >> 24) & 0xFF);
        byte r = (byte)((argb >> 16) & 0xFF);
        byte g = (byte)((argb >> 8) & 0xFF);
        byte b = (byte)(argb & 0xFF);
        return Color.FromArgb(a, r, g, b);
    }

    /// <summary>
    /// 将 <see cref="Color"/> 转换为 0xAARRGGBB 格式的无符号整数.
    /// </summary>
    /// <param name="color">颜色值.</param>
    /// <returns>ARGB 颜色值.</returns>
    public static uint ToArgbUInt(Color color) =>
        ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
}
