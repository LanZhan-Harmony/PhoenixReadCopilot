using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class SearchSectionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var sectionType = (SearchSectionType)value;
        return sectionType switch
        {
            SearchSectionType.Video => Resources.Video,
            SearchSectionType.Anime => Resources.Anime,
            SearchSectionType.Cinema => Resources.Cinema,
            SearchSectionType.User => Resources.User,
            SearchSectionType.Article => Resources.Article,
            _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
