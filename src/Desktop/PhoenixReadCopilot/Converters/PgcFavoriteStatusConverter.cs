using Microsoft.UI.Xaml.Data;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class PgcFavoriteStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = (PgcFavoriteStatus)value;
        return status switch
        {
            PgcFavoriteStatus.Want => Resources.WantWatch,
            PgcFavoriteStatus.Watching => Resources.Watching,
            PgcFavoriteStatus.Watched => Resources.Watched,
            _ => throw new Exception("Unknown status."),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
