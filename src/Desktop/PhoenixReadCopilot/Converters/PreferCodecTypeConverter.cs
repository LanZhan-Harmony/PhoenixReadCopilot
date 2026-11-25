using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 偏好解码模式转换为可读文本.
    /// </summary>
    public static string PreferCodecToText(PreferCodecType codec) => codec switch
    {
        PreferCodecType.H265 => Resources.H265,
        PreferCodecType.H264 => Resources.H264,
        PreferCodecType.Av1 => Resources.Av1,
        _ => string.Empty,
    };
}
