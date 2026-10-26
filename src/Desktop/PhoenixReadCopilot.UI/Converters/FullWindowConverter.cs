using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class FullWindowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isFullWindow = (bool)value;

        if (targetType == typeof(FluentIcons.Common.Symbol))
        {
            return isFullWindow
                ? FluentIcons.Common.Symbol.WindowAd
                : FluentIcons.Common.Symbol.Window;
        }
        else
        {
            return isFullWindow ? Resources.ExitFullWindow : Resources.EnterFullWindow;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
