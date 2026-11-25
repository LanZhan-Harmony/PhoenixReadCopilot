using Microsoft.UI;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Forms;

namespace PhoenixReadCopilot.Contracts.Services;

/// <summary>
/// 窗口管理与视窗交互服务契约.
/// </summary>
internal interface IWindowService
{
    /// <summary>
    /// 当前已激活/置顶的窗口.
    /// </summary>
    Window? ActiveWindow { get; set; }

    /// <summary>
    /// 已注册的窗口列表.
    /// </summary>
    List<Window> Windows { get; }

    /// <summary>
    /// 应用是否已关闭.
    /// </summary>
    bool IsClosed { get; set; }

    /// <summary>
    /// 新窗口注册完成时触发.
    /// </summary>
    event EventHandler<Window>? WindowRegistered;

    /// <summary>
    /// 窗口注销完成时触发.
    /// </summary>
    event EventHandler<Window>? WindowUnregistered;

    /// <summary>
    /// 注册窗口.
    /// </summary>
    void RegisterWindow(Window window);

    /// <summary>
    /// 注销窗口.
    /// </summary>
    void UnregisterWindow(Window window);

    /// <summary>
    /// 查找指定类型的窗口.
    /// </summary>
    TWindow? GetWindow<TWindow>()
        where TWindow : Window;

    /// <summary>
    /// 获取当前活动窗口的窗口标识.
    /// </summary>
    WindowId? GetActiveWindowId();

    /// <summary>
    /// 获取当前适合承载轻量提示的窗口.
    /// </summary>
    ITipWindow? GetTipWindow();

    /// <summary>
    /// 激活主窗口 (若不存在则创建并激活).
    /// </summary>
    void ActivateMainWindow();

    /// <summary>
    /// 更改所有已注册窗口的主题.
    /// </summary>
    void ChangeTheme(ElementTheme theme);

    /// <summary>
    /// 使当前（或指定）窗口进入全屏模式.
    /// </summary>
    void EnterFullScreen(Window? window = null);

    /// <summary>
    /// 使当前（或指定）窗口退出全屏模式.
    /// </summary>
    void ExitFullScreen(Window? window = null);

    /// <summary>
    /// 使当前（或指定）窗口进入重叠/画中画模式.
    /// </summary>
    void EnterOverlap(Window? window = null);

    /// <summary>
    /// 使当前（或指定）窗口退出重叠/画中画模式.
    /// </summary>
    void ExitOverlap(Window? window = null);

    /// <summary>
    /// 使当前（或指定）窗口进入紧凑浮层（小窗）模式.
    /// </summary>
    void EnterCompactOverlay(Window? window = null);

    /// <summary>
    /// 使当前（或指定）窗口退出紧凑浮层（小窗）模式.
    /// </summary>
    void ExitCompactOverlay(Window? window = null);
}
