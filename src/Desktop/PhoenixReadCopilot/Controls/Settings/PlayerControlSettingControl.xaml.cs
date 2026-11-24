using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 播放控件设置.
/// </summary>
internal sealed partial class PlayerControlSettingControl : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerControlSettingControl"/> class.
    /// </summary>
    public PlayerControlSettingControl()
    {
        InitializeComponent();
    }
}
