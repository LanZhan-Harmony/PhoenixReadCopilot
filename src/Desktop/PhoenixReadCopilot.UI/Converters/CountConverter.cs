using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Toolkits;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class CountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var count = System.Convert.ToInt64(value);
        return AppToolkit.FormatCount(count);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
