using PhoenixReadCopilot.Strings;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 全窗口模式转换为提示文本.
    /// </summary>
    public static string FullWindowTooltip(bool isFullWindow) =>
        isFullWindow ? Resources.ExitFullWindow : Resources.EnterFullWindow;

    /// <summary>
    /// 全窗口模式转换为图标.
    /// </summary>
    public static Symbol FullWindowSymbol(bool isFullWindow) =>
        isFullWindow ? Symbol.WindowAd : Symbol.Window;
}
