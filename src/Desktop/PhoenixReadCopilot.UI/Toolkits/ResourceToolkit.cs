using PhoenixReadCopilot.UI.Models.Constants;
using Richasy.WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.UI.Toolkits;

/// <summary>
/// 资源管理工具.
/// </summary>
internal sealed class ResourceToolkit : SharedResourceToolkit
{
    /// <summary>
    /// Get localized text.
    /// </summary>
    /// <param name="stringName">Resource name corresponding to localized text.</param>
    /// <returns>Localized text.</returns>
    public static string GetLocalizedString(StringNames stringName) =>
        GlobalDependencies
            .Kernel.GetRequiredService<IResourceToolkit>()
            .GetLocalizedString(stringName.ToString());
}
