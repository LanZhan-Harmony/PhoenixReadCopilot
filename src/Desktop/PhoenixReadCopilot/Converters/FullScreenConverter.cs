using PhoenixReadCopilot.Strings;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 全屏模式转换为提示文本.
    /// </summary>
    public static string FullScreenTooltip(bool isFullScreen) =>
        isFullScreen ? Resources.ExitFullScreen : Resources.EnterFullScreen;

    /// <summary>
    /// 全屏模式转换为图标.
    /// </summary>
    public static Symbol FullScreenSymbol(bool isFullScreen) =>
        isFullScreen ? Symbol.FullScreenMinimize : Symbol.FullScreenMaximize;
}
