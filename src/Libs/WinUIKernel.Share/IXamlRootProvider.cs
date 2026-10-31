using Microsoft.UI.Xaml;

namespace WinUIKernel.Share;

/// <summary>
/// Provides a way to get the XamlRoot of the control.
/// </summary>
public interface IXamlRootProvider
{
    /// <summary>
    /// Gets the XamlRoot of the control.
    /// </summary>
    public XamlRoot? XamlRoot { get; }
}
