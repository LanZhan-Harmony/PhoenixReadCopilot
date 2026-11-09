using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class GridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double v = (double)value;
        return v <= 0 ? GridLength.Auto : new GridLength(v);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
