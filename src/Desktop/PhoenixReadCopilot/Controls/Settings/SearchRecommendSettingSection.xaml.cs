using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 搜索与推荐设置分区.
/// </summary>
internal sealed partial class SearchRecommendSettingSection : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    public SearchRecommendSettingSection()
    {
        InitializeComponent();
    }
}
