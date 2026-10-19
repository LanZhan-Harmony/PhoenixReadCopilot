using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;
using Microsoft.UI.Xaml.Data;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class PgcFavoriteTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var type = (Richasy.BiliKernel.Models.PgcFavoriteType)value;
        return type switch
        {
            Richasy.BiliKernel.Models.PgcFavoriteType.Anime => ResourceToolkit.GetLocalizedString(
                StringNames.MyFavoriteAnime
            ),
            Richasy.BiliKernel.Models.PgcFavoriteType.Cinema => ResourceToolkit.GetLocalizedString(
                StringNames.MyFavoriteFilm
            ),
            _ => throw new Exception("Unknown type."),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
