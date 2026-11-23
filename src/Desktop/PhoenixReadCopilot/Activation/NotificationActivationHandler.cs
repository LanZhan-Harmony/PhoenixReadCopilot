using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.Activation;

/// <summary>
/// Windows 原生系统通知点击与按钮操作激活处理器.
/// </summary>
internal sealed class NotificationActivationHandler : IActivationHandler
{
    private readonly IWindowService _windowService;
    private readonly IAppLifecycleService _lifecycleService;
    private readonly ILogger<NotificationActivationHandler> _logger;

    public NotificationActivationHandler(
        IWindowService windowService,
        IAppLifecycleService lifecycleService,
        ILogger<NotificationActivationHandler> logger
    )
    {
        _windowService = windowService;
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    public bool CanHandle(object args)
    {
        if (args is AppActivationArguments appArgs)
        {
            return appArgs.Kind == ExtendedActivationKind.AppNotification;
        }

        if (args is LaunchActivatedEventArgs)
        {
            return AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind
                == ExtendedActivationKind.AppNotification;
        }

        return false;
    }

    public async Task HandleAsync(object args)
    {
        AppActivationArguments? activatedArgs =
            args as AppActivationArguments ?? AppInstance.GetCurrent().GetActivatedEventArgs();

        if (activatedArgs?.Data is AppNotificationActivatedEventArgs notifArgs)
        {
            IDictionary<string, string> arguments = notifArgs.Arguments;

            // 1. 处理“打开文件夹”动作按钮
            if (
                arguments.TryGetValue("action", out string? action)
                && action == "openFolder"
                && arguments.TryGetValue("folder", out string? folder)
                && !string.IsNullOrWhiteSpace(folder)
                && Directory.Exists(folder)
            )
            {
                try
                {
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{folder}\"",
                            UseShellExecute = true,
                        }
                    );
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"通过系统通知按钮打开文件夹失败: {folder}");
                }

                // 若主窗口尚未创建（说明是由通知按钮冷启动拉起的后台辅助进程），完成打开操作后直接退出
                if (App.GetMainWindow() is null)
                {
                    Application.Current.Exit();
                    return;
                }

                return;
            }
        }

        // 2. 点击通知主体卡片：激活并置顶主窗口
        if (App.GetMainWindow() is { } mainWindow)
        {
            mainWindow.Activate();
        }
        else
        {
            _lifecycleService.Launch();
            _windowService.ActivateMainWindow();
            _ = ItemServices.Account.InitializeAsync();
        }

        await Task.CompletedTask;
    }
}