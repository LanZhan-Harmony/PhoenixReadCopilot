using Microsoft.UI.Xaml.Data;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class PgcFavoriteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        PgcFavoriteType type = (PgcFavoriteType)value;
        return type switch
        {
            PgcFavoriteType.Anime => FluentIcons.Common.Symbol.Dust,
            PgcFavoriteType.Cinema => FluentIcons.Common.Symbol.MoviesAndTv,
            _ => throw new Exception("Unknown type."),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
