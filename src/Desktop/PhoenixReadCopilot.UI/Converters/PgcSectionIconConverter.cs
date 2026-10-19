using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class PgcSectionIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var sectionType = (PgcSectionType)value;
        return sectionType switch
        {
            PgcSectionType.Timeline => FluentIcons.Common.Symbol.Timeline,
            PgcSectionType.Anime => FluentIcons.Common.Symbol.Dust,
            PgcSectionType.Movie => FluentIcons.Common.Symbol.MoviesAndTv,
            PgcSectionType.TV => FluentIcons.Common.Symbol.Tv,
            PgcSectionType.Documentary => FluentIcons.Common.Symbol.EarthLeaf,
            _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
