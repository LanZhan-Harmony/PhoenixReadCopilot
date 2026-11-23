using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using PhoenixReadCopilot.Controls.Components;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.ViewModels.Core;
using ZLogger;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 跨平台/系统与应用内通知服务实现.
/// 结合 Windows AppNotification 原生通知（支持进度条、自定义应用Logo、操作按钮）与 WinUI 3 应用内状态提示.
/// </summary>
internal sealed class NotificationService : INotificationService
{
    private const string NotificationGroup = "downloads";

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
                AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
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

                await appViewModel.ShowTipAsync(message, infoType);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"显示应用内通知提示失败: {message}");
        }
    }

    /// <inheritdoc/>
    public void ShowSystemNotification(
        string title,
        string message,
        string? logoUri = null,
        string? folderPath = null
    )
    {
        if (!_isAppNotificationSupported)
        {
            return;
        }

        try
        {
            AppNotificationBuilder builder = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message);

            Uri? validLogo = TryGetValidLocalImageUri(logoUri);
            if (validLogo is not null)
            {
                builder.SetAppLogoOverride(validLogo, AppNotificationImageCrop.Default);
            }

            if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
            {
                builder.AddButton(
                    new AppNotificationButton("打开文件夹").SetInvokeUri(
                        new Uri(Path.GetFullPath(folderPath))
                    )
                );
            }

            AppNotification notification = builder.BuildNotification();
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
        string headerTitle,
        string progressBarTitle,
        string status,
        double progress,
        bool isCompleted = false,
        string? logoUri = null
    )
    {
        if (!_isAppNotificationSupported)
        {
            return;
        }

        try
        {
            double normalizedProgress = Math.Clamp(progress, 0.0, 1.0);
            uint seq = _progressSequenceMap.AddOrUpdate(tag, 1, (_, current) => current + 1);

            if (seq == 1)
            {
                // 首次展示带进度条的系统通知：顶部显示 headerTitle ("正在下载 剧名")，进度条上绑定 Title ("剧名")
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .AddText(headerTitle)
                    .AddProgressBar(
                        new AppNotificationProgressBar()
                            .BindTitle()
                            .BindValue()
                            .BindValueStringOverride()
                            .BindStatus()
                    )
                    .SetTag(tag)
                    .SetGroup(NotificationGroup);

                Uri? validLogo = TryGetValidLocalImageUri(logoUri);
                if (validLogo is not null)
                {
                    builder.SetAppLogoOverride(validLogo, AppNotificationImageCrop.Default);
                }

                AppNotification notification = builder.BuildNotification();
                notification.Progress = new AppNotificationProgressData(seq)
                {
                    Title = progressBarTitle,
                    Value = normalizedProgress,
                    ValueStringOverride = $"{Math.Round(normalizedProgress * 100)}%",
                    Status = status,
                };

                AppNotificationManager.Default.Show(notification);
            }
            else
            {
                // 后续更新已有通知的进度条数据
                AppNotificationProgressData progressData = new(seq)
                {
                    Title = progressBarTitle,
                    Value = normalizedProgress,
                    ValueStringOverride = isCompleted
                        ? "100%"
                        : $"{Math.Round(normalizedProgress * 100)}%",
                    Status = status,
                };

                _ = AppNotificationManager.Default.UpdateAsync(
                    progressData,
                    tag,
                    NotificationGroup
                );

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

    /// <inheritdoc/>
    public async Task RemoveNotificationAsync(string tag)
    {
        if (!_isAppNotificationSupported)
        {
            return;
        }

        try
        {
            await AppNotificationManager.Default.RemoveByTagAndGroupAsync(tag, NotificationGroup);
            _progressSequenceMap.TryRemove(tag, out _);
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"移除通知失败: tag={tag}");
        }
    }

    /// <inheritdoc/>
    public void RemoveNotification(string tag)
    {
        _ = RemoveNotificationAsync(tag);
    }

    private void OnNotificationInvoked(
        AppNotificationManager sender,
        AppNotificationActivatedEventArgs args
    )
    {
        try
        {
            if (
                args.Arguments.TryGetValue("action", out string? action)
                && action == "openFolder"
                && args.Arguments.TryGetValue("folder", out string? folder)
                && !string.IsNullOrWhiteSpace(folder)
                && Directory.Exists(folder)
            )
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
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"处理系统通知按钮点击打开文件夹失败");
        }
    }

    /// <summary>
    /// 注销 Windows 原生系统通知服务.
    /// </summary>
    public void Unregister()
    {
        try
        {
            if (_isAppNotificationSupported)
            {
                AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
                AppNotificationManager.Default.Unregister();
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"注销 Windows AppNotification 失败.");
        }
    }

    private static Uri? TryGetValidLocalImageUri(string? uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
        {
            return null;
        }

        try
        {
            if (File.Exists(uriString))
            {
                return new Uri(Path.GetFullPath(uriString));
            }

            if (
                Uri.TryCreate(uriString, UriKind.Absolute, out Uri? uri)
                && (uri.IsFile || uri.Scheme is "ms-appx" or "ms-appdata")
            )
            {
                return uri;
            }
        }
        catch
        {
            // 忽略无效 URI 解析
        }

        return null;
    }
}
