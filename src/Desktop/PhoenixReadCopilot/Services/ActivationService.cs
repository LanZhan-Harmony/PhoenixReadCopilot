using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Activation;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using ZLogger;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 应用程序生命周期启动与激活路由服务实现.
/// 负责管理启动前的核心服务预热、分发特定激活处理器（通知激活、协议激活、文件激活等），以及常规应用界面的启动引导.
/// </summary>
public sealed class ActivationService : IActivationService
{
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IActivationHandler _defaultHandler;
    private readonly ISettingsService _settingsService;
    private readonly AppUpdateService _appUpdateService;
    private readonly ILogger<ActivationService> _logger;

    public ActivationService(
        IEnumerable<IActivationHandler> activationHandlers,
        DefaultActivationHandler defaultHandler,
        ISettingsService settingsService,
        AppUpdateService appUpdateService,
        ILogger<ActivationService> logger
    )
    {
        _activationHandlers = activationHandlers;
        _defaultHandler = defaultHandler;
        _settingsService = settingsService;
        _appUpdateService = appUpdateService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ActivateAsync(object activationArgs)
    {
        try
        {
            // 1. 在激活前执行的预备工作（初始化设置等）
            await InitializeAsync();

            // 2. 调度匹配的 ActivationHandler 进行激活处理
            await HandleActivationAsync(activationArgs);

            // 3. 在激活之后执行的收尾任务
            await StartupAsync();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"应用程序生命周期激活过程中发生异常.");
            throw;
        }
    }

    private async Task InitializeAsync()
    {
        _ = _settingsService.Settings;
        await Task.CompletedTask;
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        IActivationHandler? activationHandler = _activationHandlers.FirstOrDefault(h =>
            h.CanHandle(activationArgs)
        );

        if (activationHandler is not null)
        {
            await activationHandler.HandleAsync(activationArgs);
            return;
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs);
        }
    }

    private async Task StartupAsync()
    {
        _ = _appUpdateService.CheckForUpdateOnStartupAsync();
        await Task.CompletedTask;
    }
}
