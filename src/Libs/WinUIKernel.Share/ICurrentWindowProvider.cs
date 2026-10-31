using Microsoft.UI.Xaml;

namespace WinUIKernel.Share;

/// <summary>
/// Interface for providing the current window.
/// </summary>
public interface ICurrentWindowProvider
{
    /// <summary>
    /// Gets the current window.
    /// </summary>
    Window CurrentWindow { get; }
}
