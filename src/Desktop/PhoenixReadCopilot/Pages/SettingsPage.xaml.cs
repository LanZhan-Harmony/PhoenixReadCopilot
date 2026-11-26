using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Pages;

/// <summary>
/// 设置页面.
/// </summary>
internal sealed partial class SettingsPage : Page
{
    /// <summary>
    /// 视图模型.
    /// </summary>
    public SettingsPageViewModel ViewModel { get; } =
        Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    public SettingsPage()
    {
        InitializeComponent();
    }
}
