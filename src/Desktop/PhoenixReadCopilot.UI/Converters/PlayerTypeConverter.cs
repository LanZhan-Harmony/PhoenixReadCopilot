using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class PlayerTypeConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var result = string.Empty;
        if (value is PlayerType type)
        {
            switch (type)
            {
                case PlayerType.Native:
                    result = Resources.Native;
                    break;
                case PlayerType.Web:
                    result = Resources.Web;
                    break;
                case PlayerType.Island:
                    result = Resources.IslandPlayer;
                    break;
                default:
                    result = "MPV（已弃用）";
                    break;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
