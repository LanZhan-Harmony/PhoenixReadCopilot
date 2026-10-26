using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class ChildCommentTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var count = (int)value;
        return string.Format(Resources.MoreReplyDisplay, count);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
