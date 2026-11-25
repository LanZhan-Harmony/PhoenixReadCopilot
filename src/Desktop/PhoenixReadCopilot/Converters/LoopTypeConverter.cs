using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 视频循环类型转换为可读文本.
    /// </summary>
    public static string LoopTypeToText(VideoLoopType loopType) => loopType switch
    {
        VideoLoopType.Single => Resources.SingleLoop,
        VideoLoopType.List => Resources.ListLoop,
        _ => Resources.NoLoop,
    };
}
