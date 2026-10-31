using PhoenixReadCopilot.UI.ViewModels.Core;
using WinUIKernel.Share;

namespace PhoenixReadCopilot.UI.Extensions;

public sealed class XamlRootProvider : IXamlRootProvider
{
    public XamlRoot? XamlRoot =>
        GlobalDependencies
            .Kernel.GetRequiredService<AppViewModel>()
            .ActivatedWindow.Content.XamlRoot;
}
