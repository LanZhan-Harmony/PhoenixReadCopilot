using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 播放器类型转换为可读文本.
    /// </summary>
    public static string PlayerTypeToText(PlayerType type) => type switch
    {
        PlayerType.FFmpeg => Resources.FFmpegPlayer,
        _ => Resources.FFmpegPlayer,
    };
}
