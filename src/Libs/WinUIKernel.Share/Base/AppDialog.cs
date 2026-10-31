using Microsoft.UI.Xaml.Controls;
using WinUIKernel.Share.Toolkits;

namespace WinUIKernel.Share.Base;

/// <summary>
/// Application dialog base class.
/// </summary>
public abstract class AppDialog : ContentDialog
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDialog"/> class.
    /// </summary>
    protected AppDialog()
    {
        XamlRoot = WinUIKernelShareExtensions.Kernel.GetRequiredService<IXamlRootProvider>().XamlRoot;
        WinUIKernelShareExtensions.Kernel.GetRequiredService<IAppToolkit>().ResetControlTheme(this);
    }
}
