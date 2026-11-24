using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class SearchSectionIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        SearchSectionType sectionType = (SearchSectionType)value;
        return sectionType switch
        {
            SearchSectionType.Drama => Symbol.VideoClip,
            SearchSectionType.User => Symbol.People,
            _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
