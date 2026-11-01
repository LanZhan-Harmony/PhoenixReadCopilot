using Microsoft.UI.Xaml;
using PhoenixReadCopilot.UI.ViewModels.Core;
using Windows.Storage;
using Windows.System;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Settings;

/// <summary>
/// 日志设置控件.
/// </summary>
public sealed partial class LoggerSettingControl : SettingsPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerSettingControl"/> class.
    /// </summary>
    public LoggerSettingControl() => InitializeComponent();

    private async void OnOpenLoggerFolderButtonClickAsync(object sender, RoutedEventArgs e)
    {
        var folder = await StorageFolder.GetFolderFromPathAsync(App.LoggerFolder).AsTask();
        _ = await Launcher.LaunchFolderAsync(folder).AsTask();
    }

    private void OnCleanLoggerButtonClickAsync(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(App.LoggerFolder))
        {
            foreach (var file in Directory.GetFiles(App.LoggerFolder))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }

        this.Get<AppViewModel>()
            .ShowTipCommand.Execute((Strings.Resources.LogEmptied, InfoType.Success));
    }
}
