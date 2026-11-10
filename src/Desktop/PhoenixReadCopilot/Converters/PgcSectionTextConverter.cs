using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class PgcSectionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        PgcSectionType sectionType = (PgcSectionType)value;
        return sectionType switch
        {
            PgcSectionType.Timeline => Resources.TimeChart,
            PgcSectionType.Movie => Resources.Movie,
            PgcSectionType.TV => Resources.FindDrama,
            PgcSectionType.Documentary => Resources.Documentary,
            _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
