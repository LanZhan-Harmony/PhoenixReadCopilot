using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 播放行为设置控件.
/// </summary>
internal sealed partial class PlayerBehaviorSettingControl : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerBehaviorSettingControl"/> class.
    /// </summary>
    public PlayerBehaviorSettingControl()
    {
        InitializeComponent();
    }
}
