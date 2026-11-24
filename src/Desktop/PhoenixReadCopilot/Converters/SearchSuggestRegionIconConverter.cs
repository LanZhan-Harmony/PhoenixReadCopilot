using Microsoft.UI.Xaml.Data;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class SearchSuggestRegionIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string? id = value as string;
        return id switch
        {
            "space" => Symbol.Person,
            "history" => Symbol.History,
            _ => Symbol.Search,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
