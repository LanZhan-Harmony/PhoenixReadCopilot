using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Forms;
using WinUIEx;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 窗口管理与视窗交互服务实现.
/// </summary>
internal sealed class WindowService : IWindowService
{
    public List<Window> Windows { get; private set; } = [];

    public Window? ActiveWindow { get; set; }

    public bool IsClosed { get; set; }

    public WindowService() { }

    public void RegisterWindow(Window window)
    {
        if (!Windows.Contains(window))
        {
            Windows.Add(window);
        }
    }

    public void UnregisterWindow(Window window)
    {
        Windows.Remove(window);
        if (ActiveWindow == window)
        {
            ActiveWindow = Windows.LastOrDefault();
        }
    }

    public TWindow? GetWindow<TWindow>()
        where TWindow : Window
    {
        return Windows.OfType<TWindow>().FirstOrDefault();
    }

    public WindowId? GetActiveWindowId()
    {
        Window? window = ActiveWindow ?? Windows.FirstOrDefault();
        return window?.AppWindow?.Id;
    }

    public ITipWindow? GetTipWindow()
    {
        return ActiveWindow as ITipWindow ?? Windows.OfType<ITipWindow>().FirstOrDefault();
    }

    public void ActivateMainWindow()
    {
        if (Windows.FirstOrDefault(p => p is MainWindow) is { } mainWindow)
        {
            mainWindow.Activate();
        }
        else
        {
            new MainWindow().Activate();
        }
    }

    public void ChangeTheme(ElementTheme theme)
    {
        foreach (Window window in Windows)
        {
            if (window.Content is FrameworkElement frameworkElement)
            {
                frameworkElement.RequestedTheme = theme;
            }
        }
    }

    public void EnterFullScreen(Window? window = null)
    {
        Window? target = window ?? ActiveWindow;
        if (target is null)
        {
            return;
        }

        target.SetWindowPresenter(AppWindowPresenterKind.FullScreen);
        if (target is IPlayerHostWindow hostWindow)
        {
            hostWindow.EnterPlayerHostMode(PlayerDisplayMode.FullScreen);
        }
    }

    public void ExitFullScreen(Window? window = null)
    {
        Window? target = window ?? ActiveWindow;
        if (target is null)
        {
            return;
        }

        target.SetWindowPresenter(AppWindowPresenterKind.Default);
        if (target is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }

    public void EnterOverlap(Window? window = null)
    {
        Window? target = window ?? ActiveWindow;
        if (target is null)
        {
            return;
        }

        if (target.AppWindow.Presenter is not OverlappedPresenter)
        {
            target.SetWindowPresenter(AppWindowPresenterKind.Overlapped);
        }

        if (target is IPlayerHostWindow hostWindow)
        {
            hostWindow.EnterPlayerHostMode(PlayerDisplayMode.FullWindow);
        }
    }

    public void ExitOverlap(Window? window = null)
    {
        Window? target = window ?? ActiveWindow;
        if (target is null)
        {
            return;
        }

        target.SetWindowPresenter(AppWindowPresenterKind.Default);
        if (target is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }

    public void EnterCompactOverlay(Window? window = null)
    {
        Window? target = window ?? ActiveWindow;
        if (target is null)
        {
            return;
        }

        if (target is WindowEx windowEx)
        {
            windowEx.MinHeight = 320;
            windowEx.MinWidth = 560;
        }

        target.SetWindowPresenter(AppWindowPresenterKind.CompactOverlay);
        target.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        if (target is IPlayerHostWindow hostWindow)
        {
            hostWindow.EnterPlayerHostMode(PlayerDisplayMode.CompactOverlay);
        }
    }

    public void ExitCompactOverlay(Window? window = null)
    {
        Window? target = window ?? ActiveWindow;
        if (target is null)
        {
            return;
        }

        if (target is WindowEx windowEx)
        {
            windowEx.MinHeight = 480;
            windowEx.MinWidth = 640;
        }

        target.SetWindowPresenter(AppWindowPresenterKind.Default);
        target.AppWindow.TitleBar.PreferredHeightOption =
            target is MainWindow ? TitleBarHeightOption.Tall : TitleBarHeightOption.Standard;
        if (target is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }
}
