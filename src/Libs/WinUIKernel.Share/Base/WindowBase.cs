using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media;

namespace WinUIKernel.Share.Base;

/// <summary>
/// 窗口基类.
/// </summary>
public abstract class WindowEx : WinUIEx.WindowEx
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowEx"/> class.
    /// </summary>
    protected WindowEx()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        SystemBackdrop = new MicaBackdrop();
    }
}
