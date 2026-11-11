using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class SearchSectionTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        SearchSectionType sectionType = (SearchSectionType)value;
        return sectionType switch
        {
            SearchSectionType.Video => Resources.Video,
            SearchSectionType.Cinema => Resources.FindDrama,
            SearchSectionType.User => Resources.User,
            _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
