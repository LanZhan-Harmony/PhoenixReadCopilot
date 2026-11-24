using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 下载设置控件.
/// </summary>
internal sealed partial class DownloadSettingControl : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    public DownloadSettingControl()
    {
        InitializeComponent();
    }
}
