using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;
using Microsoft.UI.Xaml.Data;
using Richasy.BiliKernel.Models.User;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class NotifyMessageTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var type = (NotifyMessageType)value;
        return type switch
        {
            NotifyMessageType.Reply => ResourceToolkit.GetLocalizedString(StringNames.ReplyMe),
            NotifyMessageType.At => ResourceToolkit.GetLocalizedString(StringNames.AtMe),
            NotifyMessageType.Like => ResourceToolkit.GetLocalizedString(StringNames.LikeMe),
            _ => throw new Exception("Unknown type."),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
