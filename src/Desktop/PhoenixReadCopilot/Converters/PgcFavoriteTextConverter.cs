using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Strings;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class PgcFavoriteTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        PgcFavoriteType type = (PgcFavoriteType)value;
        return type switch
        {
            _ => throw new Exception("Unknown type."),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
