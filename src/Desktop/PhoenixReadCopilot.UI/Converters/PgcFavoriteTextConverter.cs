using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Strings;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class PgcFavoriteTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var type = (PgcFavoriteType)value;
        return type switch
        {
            PgcFavoriteType.Anime => Resources.MyFavoriteAnime,
            PgcFavoriteType.Cinema => Resources.MyFavoriteFilm,
            _ => throw new Exception("Unknown type."),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
