using Microsoft.UI.Xaml;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Activation;

/// <summary>
/// 默认常规启动激活处理器 (当无其它特定 Handler 接管时执行).
/// </summary>
internal sealed class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly AppViewModel _appViewModel;

    public DefaultActivationHandler(AppViewModel appViewModel)
    {
        _appViewModel = appViewModel;
    }

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args) => true;

    protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        _appViewModel.Launch();
        await Task.CompletedTask;
    }
}
