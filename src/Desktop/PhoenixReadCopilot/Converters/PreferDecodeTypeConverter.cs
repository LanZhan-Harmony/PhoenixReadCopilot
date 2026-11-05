using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal sealed partial class PreferDecodeTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var decode = (PreferDecodeType)value;
        return decode switch
        {
            PreferDecodeType.Auto => Resources.Automatic,
            PreferDecodeType.D3D11 => Resources.D3D11Decode,
            PreferDecodeType.NVDEC => Resources.NVDECDecode,
            PreferDecodeType.Vulkan => Resources.VulkanDecode,
            PreferDecodeType.DXVA2 => Resources.DXVA2Decode,
            PreferDecodeType.Custom => Resources.Custom,
            _ => string.Empty,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
