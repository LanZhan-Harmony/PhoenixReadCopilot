using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Strings;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class FullWindowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isFullWindow = (bool)value;

        if (targetType == typeof(Symbol))
        {
            return isFullWindow
                ? Symbol.WindowAd
                : Symbol.Window;
        }
        else
        {
            return isFullWindow ? Resources.ExitFullWindow : Resources.EnterFullWindow;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
