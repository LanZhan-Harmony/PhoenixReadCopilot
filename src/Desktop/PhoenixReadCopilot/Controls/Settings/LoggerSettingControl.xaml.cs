using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Core;
using Windows.Storage;
using Windows.System;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 日志设置控件.
/// </summary>
public sealed partial class LoggerSettingControl : SettingsPageControlBase
{
    private readonly AppViewModel _appViewModel = Ioc.Default.GetRequiredService<AppViewModel>();

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
            .GetFolderFromPathAsync(PathHelper.LogFolderPath)
            .AsTask();
        _ = await Launcher.LaunchFolderAsync(folder).AsTask();
    }

    private void OnCleanLoggerButtonClickAsync(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(PathHelper.LogFolderPath))
        {
            foreach (string file in Directory.GetFiles(PathHelper.LogFolderPath))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        _appViewModel.ShowTipCommand.Execute((Strings.Resources.LogEmptied, InfoType.Success));
    }
}
