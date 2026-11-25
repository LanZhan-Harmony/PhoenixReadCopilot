using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 播放器显示模式转换为可读文本.
    /// </summary>
    public static string DisplayModeToText(PlayerDisplayMode mode) => mode switch
    {
        PlayerDisplayMode.Default => Resources.Default,
        PlayerDisplayMode.FullScreen => Resources.FullScreenMode,
        PlayerDisplayMode.FullWindow => Resources.FullWindowMode,
        PlayerDisplayMode.CompactOverlay => Resources.CompactOverlayMode,
        PlayerDisplayMode.NewWindow => Resources.NewWindow,
        _ => string.Empty,
    };
}
