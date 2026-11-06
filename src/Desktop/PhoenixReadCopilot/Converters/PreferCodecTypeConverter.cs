using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

/// <summary>
/// 偏好解码模式到可读文本转换器.
/// </summary>
internal sealed partial class PreferCodecTypeConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var result = string.Empty;
        if (value is PreferCodecType codec)
        {
            switch (codec)
            {
                case PreferCodecType.H265:
                    result = Resources.H265;
                    break;
                case PreferCodecType.H264:
                    result = Resources.H264;
                    break;
                case PreferCodecType.Av1:
                    result = Resources.Av1;
                    break;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
