using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 窗口材质设置控件.
/// </summary>
internal sealed partial class MaterialSettingControl : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    public MaterialSettingControl()
    {
        InitializeComponent();
    }
}
