using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class DisplayModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var result = string.Empty;
        if (value is PlayerDisplayMode mode)
        {
            switch (mode)
            {
                case PlayerDisplayMode.Default:
                    result = Resources.Default;
                    break;
                case PlayerDisplayMode.FullScreen:
                    result = Resources.FullScreenMode;
                    break;
                case PlayerDisplayMode.FullWindow:
                    result = Resources.FullWindowMode;
                    break;
                case PlayerDisplayMode.CompactOverlay:
                    result = Resources.CompactOverlayMode;
                    break;
                case PlayerDisplayMode.NewWindow:
                    result = Resources.NewWindow;
                    break;
            }
        }

        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
