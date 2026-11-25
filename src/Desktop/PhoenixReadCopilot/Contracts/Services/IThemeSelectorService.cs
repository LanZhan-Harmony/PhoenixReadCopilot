using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Contracts.Services;

/// <summary>
/// 应用主题选择与多窗口同步服务契约.
/// </summary>
internal interface IThemeSelectorService
{
    /// <summary>
    /// 当前设置的应用主题.
    /// </summary>
    AppTheme Theme { get; }

    /// <summary>
    /// 转换后的 WinUI 请求主题.
    /// </summary>
    ElementTheme RequestedElementTheme { get; }

    /// <summary>
    /// 当前实际生效的是否为深色主题.
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>
    /// 实际生效主题变更事件 (参数为当前是否为深色主题).
    /// </summary>
    event EventHandler<bool>? ThemeChanged;

    /// <summary>
    /// 初始化主题服务并同步已注册窗口.
    /// </summary>
    void Initialize();

    /// <summary>
    /// 切换全局应用主题.
    /// </summary>
    /// <param name="theme">目标主题.</param>
    void SetTheme(AppTheme theme);

    /// <summary>
    /// 将当前主题应用到指定窗口.
    /// </summary>
    /// <param name="window">目标窗口.</param>
    void ApplyTheme(Window window);
}
