using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace PhoenixReadCopilot.Converters;

/// <summary>
/// 将链接转换为位图.
/// </summary>
public sealed partial class UriToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value is string url && !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return new BitmapImage(uri);
        }
        else if (value is Uri validUri)
        {
            return new BitmapImage(validUri);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
