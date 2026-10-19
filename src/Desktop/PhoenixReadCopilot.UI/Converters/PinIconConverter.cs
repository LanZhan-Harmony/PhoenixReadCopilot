using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;

namespace PhoenixReadCopilot.UI.Converters;

internal sealed partial class PinIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is PinContentType type
            ? type switch
            {
                PinContentType.Pgc => FluentIcons.Common.Symbol.Dust,
                PinContentType.User => FluentIcons.Common.Symbol.Person,
                PinContentType.Video => FluentIcons.Common.Symbol.VideoClip,
                _ => FluentIcons.Common.Symbol.DocumentBulletList,
            }
            : (object)FluentIcons.Common.Symbol.Question;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
