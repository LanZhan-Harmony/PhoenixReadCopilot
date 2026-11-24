using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class PinIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is PinType type
            ? type switch
            {
                PinType.User => Symbol.Person,
                PinType.Drama => Symbol.VideoClip,
                PinType.Video => Symbol.Video,
                PinType.Post => Symbol.Document,
                PinType.Novel => Symbol.Book,
                _ => Symbol.DocumentBulletList,
            }
            : Symbol.Question;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
