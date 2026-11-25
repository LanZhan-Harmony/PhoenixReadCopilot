using Microsoft.UI.Xaml.Media.Imaging;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 将链接字符串转换为位图.
    /// </summary>
    public static BitmapImage? StringToBitmap(string? url) =>
        !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            ? new BitmapImage(uri)
            : null;

    /// <summary>
    /// 将 Uri 转换为位图.
    /// </summary>
    public static BitmapImage? UriToBitmap(Uri? uri) =>
        uri is not null ? new BitmapImage(uri) : null;
}
