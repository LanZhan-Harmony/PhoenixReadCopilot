using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

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
