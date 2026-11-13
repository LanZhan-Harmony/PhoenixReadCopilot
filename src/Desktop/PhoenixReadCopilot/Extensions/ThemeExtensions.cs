using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Extensions;

/// <summary>
/// 主题转换扩展.
/// </summary>
public static class ThemeExtensions
{
    /// <summary>
    /// 将 Core.Enums.AppTheme 转换为 WinUI ElementTheme.
    /// </summary>
    public static ElementTheme ToElementTheme(this AppTheme theme) => theme switch
    {
        AppTheme.Light => ElementTheme.Light,
        AppTheme.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };

    /// <summary>
    /// 将 WinUI ElementTheme 转换为 Core.Enums.AppTheme.
    /// </summary>
    public static AppTheme ToAppTheme(this ElementTheme theme) => theme switch
    {
        ElementTheme.Light => AppTheme.Light,
        ElementTheme.Dark => AppTheme.Dark,
        _ => AppTheme.Default,
    };
}
