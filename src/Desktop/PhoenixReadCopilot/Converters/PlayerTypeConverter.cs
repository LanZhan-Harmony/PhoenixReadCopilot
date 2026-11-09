using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class PlayerTypeConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string result = string.Empty;
        if (value is PlayerType type)
        {
            switch (type)
            {
                case PlayerType.FFmpeg:
                    result = Resources.FFmpegPlayer;
                    break;
                case PlayerType.Web:
                    result = Resources.Web;
                    break;
                default:
                    result = Resources.FFmpegPlayer;
                    break;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
