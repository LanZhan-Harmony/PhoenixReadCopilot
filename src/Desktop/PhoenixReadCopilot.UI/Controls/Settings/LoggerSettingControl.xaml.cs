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

    private async void OnCleanLoggerButtonClickAsync(object sender, RoutedEventArgs e)
    {
        var folder = await StorageFolder.GetFolderFromPathAsync(App.LoggerFolder).AsTask();
        await folder.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask();
        _ = await ApplicationData
            .Current.LocalFolder.CreateFolderAsync("Logger", CreationCollisionOption.OpenIfExists)
            .AsTask();
        this.Get<AppViewModel>()
            .ShowTipCommand.Execute((Strings.Resources.LogEmptied, InfoType.Success));
    }
}
