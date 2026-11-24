using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Strings;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

/// <summary>
/// 全屏图标及文本转换器.
/// </summary>
internal sealed partial class FullScreenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isFullScreen = (bool)value;

        if (targetType == typeof(Symbol))
        {
            return isFullScreen ? Symbol.FullScreenMinimize : Symbol.FullScreenMaximize;
        }
        else
        {
            return isFullScreen ? Resources.ExitFullScreen : Resources.EnterFullScreen;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
