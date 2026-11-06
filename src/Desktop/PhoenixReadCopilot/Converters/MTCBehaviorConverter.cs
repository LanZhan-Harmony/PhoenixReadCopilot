using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Converters;

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
