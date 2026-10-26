using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

/// <summary>
/// 全屏图标及文本转换器.
/// </summary>
internal sealed partial class FullScreenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isFullScreen = (bool)value;

        if (targetType == typeof(FluentIcons.Common.Symbol))
        {
            return isFullScreen
                ? FluentIcons.Common.Symbol.FullScreenMinimize
                : FluentIcons.Common.Symbol.FullScreenMaximize;
        }
        else
        {
            return isFullScreen ? Resources.ExitFullScreen : Resources.EnterFullScreen;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
