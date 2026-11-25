using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 偏好画质转换为可读文本.
    /// </summary>
    public static string PreferQualityToText(PreferQualityType quality) => quality switch
    {
        PreferQualityType.HD => Resources.HDFirst,
        PreferQualityType.High => Resources.PreferHighQuality,
        PreferQualityType.UHD => Resources.UHDFirst,
        _ => Resources.Automatic,
    };
}
