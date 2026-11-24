using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 通知设置控件.
/// </summary>
internal sealed partial class NotificationSettingControl : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    public NotificationSettingControl()
    {
        InitializeComponent();
    }
}
