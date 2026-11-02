using Microsoft.UI.Xaml.Media;

namespace WinUIKernel.Share.Toolkits;

/// <summary>
/// 资源管理工具.
/// </summary>
public interface IResourceToolkit
{
    /// <summary>
    /// Get theme brush.
    /// </summary>
    Brush GetThemeBrush(string brushName);
}
