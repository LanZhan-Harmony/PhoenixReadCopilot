using PhoenixReadCopilot.Strings;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 小窗模式转换为提示文本.
    /// </summary>
    public static string CompactOverlayTooltip(bool isCompact) =>
        isCompact ? Resources.ExitCompactOverlay : Resources.EnterCompactOverlay;

    /// <summary>
    /// 小窗模式转换为图标.
    /// </summary>
    public static Symbol CompactOverlaySymbol(bool isCompact) =>
        isCompact ? Symbol.ContractDownLeft : Symbol.ContractUpRight;
}
