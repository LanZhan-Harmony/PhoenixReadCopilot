using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.Base;
using ZLogger;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 跨平台/系统与应用内通知服务实现.
/// 结合 Windows AppNotification 原生通知与 WinUI 3 应用内状态提示.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationService> _logger;
    private readonly bool _isAppNotificationSupported;
    private readonly ConcurrentDictionary<string, uint> _progressSequenceMap = new();

    public NotificationService(
        IServiceProvider serviceProvider,
        ILogger<NotificationService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        try
        {
            _isAppNotificationSupported = AppNotificationManager.IsSupported();
            if (_isAppNotificationSupported)
            {
                AppNotificationManager.Default.Register();
            }
        }
        catch (Exception ex)
        {
            _isAppNotificationSupported = false;
            _logger.ZLogWarning(ex, $"初始化 Windows AppNotification 失败，将仅使用应用内通知.");
        }
    }

    /// <inheritdoc/>
    public async Task ShowInAppNotificationAsync(
        string message,
        NotificationType type = NotificationType.Information
    )
    {
        try
        {
            AppViewModel? appViewModel = _serviceProvider.GetService<AppViewModel>();
            if (appViewModel is not null)
            {
                InfoType infoType = type switch
                {
                    NotificationType.Success => InfoType.Success,
                    NotificationType.Warning => InfoType.Warning,
                    NotificationType.Error => InfoType.Error,
                    _ => InfoType.Information,
                };

                appViewModel.ShowTipCommand.Execute((message, infoType));
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"显示应用内通知提示失败: {message}");
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void ShowSystemNotification(string title, string message)
    {
        if (!_isAppNotificationSupported)
        {
            return;
        }

        try
        {
            AppNotification notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"发送 Windows 原生系统通知失败: {title}");
        }
    }

    /// <inheritdoc/>
    public void UpdateProgressNotification(
        string tag,
        string title,
        string message,
        double progress,
        bool isCompleted = false
    )
    {
        if (!_isAppNotificationSupported)
        {
            return;
        }

        try
        {
            // progress 归一化在 0.0 ~ 1.0 之间
            double normalizedProgress = Math.Clamp(progress, 0.0, 1.0);
            uint seq = _progressSequenceMap.AddOrUpdate(tag, 1, (_, current) => current + 1);

            if (seq == 1)
            {
                // 首次展示带进度条的系统通知
                AppNotification notification = new AppNotificationBuilder()
                    .AddText(title)
                    .AddProgressBar(
                        new AppNotificationProgressBar()
                            .BindTitle()
                            .BindValue()
                            .BindValueStringOverride()
                            .BindStatus()
                    )
                    .SetTag(tag)
                    .SetGroup("downloads")
                    .BuildNotification();

                notification.Progress = new AppNotificationProgressData(seq)
                {
                    Title = title,
                    Value = normalizedProgress,
                    ValueStringOverride = $"{Math.Round(normalizedProgress * 100)}%",
                    Status = message,
                };

                AppNotificationManager.Default.Show(notification);
            }
            else
            {
                // 后续更新已有通知的进度条数据
                AppNotificationProgressData progressData = new(seq)
                {
                    Title = title,
                    Value = normalizedProgress,
                    ValueStringOverride = isCompleted
                        ? "100%"
                        : $"{Math.Round(normalizedProgress * 100)}%",
                    Status = message,
                };

                _ = AppNotificationManager.Default.UpdateAsync(progressData, tag, "downloads");

                if (isCompleted)
                {
                    _progressSequenceMap.TryRemove(tag, out _);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"更新进度通知失败: tag={tag}");
        }
    }
}
