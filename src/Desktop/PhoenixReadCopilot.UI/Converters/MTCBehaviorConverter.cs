using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class MTCBehaviorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var behavior = (MTCBehavior)value;
        return behavior switch
        {
            MTCBehavior.Automatic => Resources.Automatic,
            MTCBehavior.Manual => Resources.Manual,
            _ => string.Empty,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
