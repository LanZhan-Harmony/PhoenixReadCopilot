using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Activation;

/// <summary>
/// 默认常规启动激活处理器 (作为责任链末尾的默认兜底处理器，直接调度生命周期与主窗口激活).
/// </summary>
internal sealed class DefaultActivationHandler(
    IWindowService windowService,
    IAppLifecycleService lifecycleService
) : IActivationHandler
{
    private readonly IWindowService _windowService = windowService;
    private readonly IAppLifecycleService _lifecycleService = lifecycleService;

    public bool CanHandle(object args) => true;

    public async Task HandleAsync(object args)
    {
        // 1. 基础生命周期广播与准备：清除徽标角标，广播全局初始加载中状态
        _lifecycleService.Launch();

        // 2. 调度主窗口激活与呈现
        _windowService.ActivateMainWindow();

        // 3. 触发账号数据与会话全局初始化
        _ = ItemServices.Account.InitializeAsync();

        await Task.CompletedTask;
    }
}
