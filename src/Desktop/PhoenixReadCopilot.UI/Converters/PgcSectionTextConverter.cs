using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class PgcSectionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var sectionType = (PgcSectionType)value;
        return sectionType switch
        {
            PgcSectionType.Timeline => Resources.TimeChart,
            PgcSectionType.Anime => Resources.Anime,
            PgcSectionType.Movie => Resources.Movie,
            PgcSectionType.TV => Resources.TV,
            PgcSectionType.Documentary => Resources.Documentary,
            _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
