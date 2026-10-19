using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;
using Microsoft.UI.Xaml.Data;

namespace PhoenixReadCopilot.UI.Converters;

/// <summary>
/// 循环类型转换器.
/// </summary>
internal sealed partial class LoopTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var key = (VideoLoopType)value switch
        {
            VideoLoopType.Single => StringNames.SingleLoop,
            VideoLoopType.List => StringNames.ListLoop,
            _ => StringNames.NoLoop,
        };

        return ResourceToolkit.GetLocalizedString(key);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
