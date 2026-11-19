using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class PinIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is PinType type
            ? type switch
            {
                PinType.User => FluentIcons.Common.Symbol.Person,
                PinType.Drama => FluentIcons.Common.Symbol.VideoClip,
                PinType.Video => FluentIcons.Common.Symbol.Video,
                PinType.Post => FluentIcons.Common.Symbol.Document,
                PinType.Novel => FluentIcons.Common.Symbol.Book,
                _ => FluentIcons.Common.Symbol.DocumentBulletList,
            }
            : FluentIcons.Common.Symbol.Question;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
