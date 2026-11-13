using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Controls.Settings;

/// <summary>
/// 主题设置控件.
/// </summary>
public sealed partial class ThemeSettingControl : SettingsPageControlBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeSettingControl"/> class.
    /// </summary>
    public ThemeSettingControl()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
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
