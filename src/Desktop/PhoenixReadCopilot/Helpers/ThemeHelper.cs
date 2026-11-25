using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Helpers;

internal static class ThemeHelper
{
    /// <summary>
    /// 将 Core.Enums.AppTheme 转换为 WinUI ElementTheme.
    /// </summary>
    internal static ElementTheme ToElementTheme(AppTheme theme) =>
        theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };

    /// <summary>
    /// 将 WinUI ElementTheme 转换为 Core.Enums.AppTheme.
    /// </summary>
    internal static AppTheme ToAppTheme(ElementTheme theme) =>
        theme switch
        {
            ElementTheme.Light => AppTheme.Light,
            ElementTheme.Dark => AppTheme.Dark,
            _ => AppTheme.Default,
        };
}
