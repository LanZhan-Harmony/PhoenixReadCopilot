using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

/// <summary>
/// 循环类型转换器.
/// </summary>
internal sealed partial class LoopTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (VideoLoopType)value switch
        {
            VideoLoopType.Single => Resources.SingleLoop,
            VideoLoopType.List => Resources.ListLoop,
            _ => Resources.NoLoop,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
