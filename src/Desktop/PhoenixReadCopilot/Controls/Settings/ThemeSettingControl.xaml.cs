using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 主题设置控件.
/// </summary>
internal sealed partial class ThemeSettingControl : UserControl
{
    public SettingsPageViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<SettingsPageViewModel>();

    public ThemeSettingControl()
    {
        InitializeComponent();
        Loaded += (_, _) => OnControlLoaded();
    }

    private void OnControlLoaded()
    {
        ThemePicker.SelectedIndex = ViewModel.AppTheme switch
        {
            AppTheme.Light => 0,
            AppTheme.Dark => 1,
            _ => 2,
        };
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AppTheme theme = ThemePicker.SelectedIndex switch
        {
            0 => AppTheme.Light,
            1 => AppTheme.Dark,
            _ => AppTheme.Default,
        };

        ViewModel.Settings.AppTheme = theme;
    }
}
