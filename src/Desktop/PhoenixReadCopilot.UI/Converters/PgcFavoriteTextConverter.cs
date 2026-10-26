using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class PgcFavoriteTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var type = (Richasy.BiliKernel.Models.PgcFavoriteType)value;
        return type switch
        {
            Richasy.BiliKernel.Models.PgcFavoriteType.Anime => Resources.MyFavoriteAnime,
            Richasy.BiliKernel.Models.PgcFavoriteType.Cinema => Resources.MyFavoriteFilm,
            _ => throw new Exception("Unknown type."),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
