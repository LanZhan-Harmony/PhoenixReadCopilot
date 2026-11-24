using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 布局设置分区.
/// </summary>
internal sealed partial class LayoutSettingSection : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutSettingSection"/> class.
    /// </summary>
    public LayoutSettingSection()
    {
        InitializeComponent();
    }
}
