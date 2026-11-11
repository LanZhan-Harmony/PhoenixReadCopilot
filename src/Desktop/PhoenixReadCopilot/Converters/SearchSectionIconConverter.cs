using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class SearchSectionIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        SearchSectionType sectionType = (SearchSectionType)value;
        return sectionType switch
        {
            SearchSectionType.Video => FluentIcons.Common.Symbol.VideoClip,
            SearchSectionType.Anime => FluentIcons.Common.Symbol.Dust,
            SearchSectionType.User => FluentIcons.Common.Symbol.People,
            SearchSectionType.Cinema => FluentIcons.Common.Symbol.MoviesAndTv,
            _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
