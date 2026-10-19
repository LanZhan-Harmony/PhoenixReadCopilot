using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;
using Microsoft.UI.Xaml.Data;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class PgcSectionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var sectionType = (PgcSectionType)value;
        var lanName = sectionType switch
        {
            PgcSectionType.Timeline => StringNames.TimeChart,
            PgcSectionType.Anime => StringNames.Anime,
            PgcSectionType.Movie => StringNames.Movie,
            PgcSectionType.TV => StringNames.TV,
            PgcSectionType.Documentary => StringNames.Documentary,
            _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
        };

        return ResourceToolkit.GetLocalizedString(lanName);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
