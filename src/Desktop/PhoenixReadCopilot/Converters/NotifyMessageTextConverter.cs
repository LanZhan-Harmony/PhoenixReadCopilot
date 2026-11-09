using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Strings;
using Richasy.BiliKernel.Models.User;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class NotifyMessageTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        NotifyMessageType type = (NotifyMessageType)value;
        return type switch
        {
            NotifyMessageType.Reply => Resources.ReplyMe,
            NotifyMessageType.At => Resources.AtMe,
            NotifyMessageType.Like => Resources.LikeMe,
            _ => throw new Exception("Unknown type."),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
