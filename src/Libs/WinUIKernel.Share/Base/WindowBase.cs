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
        AppWindow.TitleBar.IconShowOptions = Microsoft
            .UI
            .Windowing
            .IconShowOptions
            .HideIconAndSystemMenu;
        SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
    }
}
