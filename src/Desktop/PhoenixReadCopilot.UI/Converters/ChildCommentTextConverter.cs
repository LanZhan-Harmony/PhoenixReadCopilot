using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class ChildCommentTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var count = (int)value;
        return string.Format(
            ResourceToolkit.GetLocalizedString(StringNames.MoreReplyDisplay),
            count
        );
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
