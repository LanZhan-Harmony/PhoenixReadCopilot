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

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationSettingControl"/> class.
    /// </summary>
    public NotificationSettingControl()
    {
        InitializeComponent();
    }
}
