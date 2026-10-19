using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class PgcFavoriteStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = (PgcFavoriteStatus)value;
        return status switch
        {
            PgcFavoriteStatus.Want => ResourceToolkit.GetLocalizedString(StringNames.WantWatch),
            PgcFavoriteStatus.Watching => ResourceToolkit.GetLocalizedString(StringNames.Watching),
            PgcFavoriteStatus.Watched => ResourceToolkit.GetLocalizedString(StringNames.Watched),
            _ => throw new Exception("Unknown status."),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
