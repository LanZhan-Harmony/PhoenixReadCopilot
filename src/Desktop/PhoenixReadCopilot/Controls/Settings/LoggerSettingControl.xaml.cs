using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.ViewModels.View;
using Windows.Storage;
using Windows.System;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 日志设置控件.
/// </summary>
internal sealed partial class LoggerSettingControl : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    private readonly INotificationService _notificationService =
        Ioc.Default.GetRequiredService<INotificationService>();
    private readonly IPathService _pathService = Ioc.Default.GetRequiredService<IPathService>();

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerSettingControl"/> class.
    /// </summary>
    public LoggerSettingControl()
    {
        InitializeComponent();
    }

    private async void OnOpenLoggerFolderButtonClickAsync(object sender, RoutedEventArgs e)
    {
        StorageFolder folder = await StorageFolder
            .GetFolderFromPathAsync(_pathService.LogFolderPath)
            .AsTask();
        _ = await Launcher.LaunchFolderAsync(folder).AsTask();
    }

    private void OnCleanLoggerButtonClickAsync(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(_pathService.LogFolderPath))
        {
            foreach (string file in Directory.GetFiles(_pathService.LogFolderPath))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        _notificationService.ShowInAppNotification(
            Strings.Resources.LogEmptied,
            NotificationType.Success
        );
    }
}
