using Microsoft.UI.Xaml;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share;

namespace PhoenixReadCopilot.Extensions;

public sealed class XamlRootProvider : IXamlRootProvider
{
    private readonly AppViewModel _appViewModel;

    public XamlRootProvider(AppViewModel appViewModel)
    {
        _appViewModel = appViewModel;
    }

    public XamlRoot? XamlRoot =>
        _appViewModel.ActivatedWindow?.Content?.XamlRoot;
}
