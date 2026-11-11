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
            result = type switch
            {
                PlayerType.FFmpeg => Resources.FFmpegPlayer,
                PlayerType.Web => Resources.Web,
                _ => Resources.FFmpegPlayer,
            };
        }

        return result;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
