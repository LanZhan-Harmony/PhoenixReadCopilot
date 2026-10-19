using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class LevelImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        new BitmapImage(new Uri($"ms-appx:///Assets/Level/level_{(int?)value ?? 0}.png"));

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
