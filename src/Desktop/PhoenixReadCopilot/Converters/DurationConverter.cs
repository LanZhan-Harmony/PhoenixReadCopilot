using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Helpers;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class DurationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int d)
        {
            return FormatHelper.FormatDuration(TimeSpan.FromSeconds(d));
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
