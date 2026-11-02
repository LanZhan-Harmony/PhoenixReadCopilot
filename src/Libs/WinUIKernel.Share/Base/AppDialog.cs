using CommunityToolkit.Mvvm.DependencyInjection;
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
        XamlRoot = Ioc.Default.GetRequiredService<IXamlRootProvider>().XamlRoot;
        Ioc.Default.GetRequiredService<IAppToolkit>().ResetControlTheme(this);
    }
}
