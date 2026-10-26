using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;

namespace PhoenixReadCopilot.UI.Converters;

/// <summary>
/// 偏好画质的可读文本转换器.
/// </summary>
internal sealed partial class PreferQualityTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is PreferQualityType quality
            ? quality switch
            {
                PreferQualityType.HD => Resources.HDFirst,
                PreferQualityType.High => Resources.PreferHighQuality,
                PreferQualityType.UHD => Resources.UHDFirst,
                _ => Resources.Automatic,
            }
            : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
