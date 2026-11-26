using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.Storage.Pickers;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Strings;
using Windows.System;
using Windows.UI;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 设置页面视图模型.
/// </summary>
internal sealed partial class SettingsPageViewModel : ViewModelBase
{
    private readonly IWindowService _windowService;
    private readonly INavigationService _navigationService;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IMaterialSelectorService _materialSelectorService;
    public AppSettings Settings;

    /// <summary>
    /// 当前应用主题.
    /// </summary>
    public AppTheme AppTheme => Settings.AppTheme;

    /// <summary>
    /// 主题显示文本.
    /// </summary>
    public string AppThemeText =>
        AppTheme switch
        {
            AppTheme.Light => Resources.LightTheme,
            AppTheme.Dark => Resources.DarkTheme,
            _ => Resources.SystemDefault,
        };

    /// <summary>
    /// 是否显示顶部导航栏.
    /// </summary>
    public bool IsTopNavShown => Settings.IsTopNavBarShown;

    /// <summary>
    /// 可选窗口材质列表.
    /// </summary>
    public List<BackdropMaterialType> BackdropMaterialCollection { get; } =
    [.. Enum.GetValues<BackdropMaterialType>()];

    /// <summary>
    /// 当前选中的窗口材质.
    /// </summary>
    public BackdropMaterialType SelectedBackdropMaterial
    {
        get => Settings.BackdropMaterial;
        set
        {
            if (Settings.BackdropMaterial != value)
            {
                _materialSelectorService.SetMaterial(value);
            }
        }
    }

    /// <summary>
    /// 窗口失焦后是否停止背景更新.
    /// </summary>
    public bool IsBackdropFallbackEnabled
    {
        get => Settings.IsBackdropFallbackEnabled;
        set
        {
            if (Settings.IsBackdropFallbackEnabled != value)
            {
                _materialSelectorService.SetFallbackEnabled(value);
            }
        }
    }

    /// <summary>
    /// 窗口背景不透明度 (0-100).
    /// </summary>
    public double BackdropLuminosityOpacity
    {
        get => Settings.BackdropLuminosityOpacity;
        set
        {
            byte clamped = (byte)Math.Clamp(Math.Round(value), 0, 100);
            if (Settings.BackdropLuminosityOpacity != clamped)
            {
                _materialSelectorService.SetLuminosityOpacity(clamped);
            }
        }
    }

    /// <summary>
    /// 窗口背景着色颜色.
    /// </summary>
    public Color BackdropTintColor
    {
        get => _materialSelectorService.TintColor;
        set
        {
            if (_materialSelectorService.TintColor != value)
            {
                _materialSelectorService.SetTintColor(value);
            }
        }
    }

    /// <summary>
    /// 当前材质是否支持失焦回退配置.
    /// </summary>
    public bool SupportsBackdropFallback => _materialSelectorService.SupportsFallback;

    /// <summary>
    /// 当前材质是否支持不透明度调节.
    /// </summary>
    public bool SupportsBackdropLuminosityOpacity =>
        _materialSelectorService.SupportsLuminosityOpacity;

    /// <summary>
    /// 当前材质是否支持自定义背景颜色.
    /// </summary>
    public bool SupportsBackdropTintColor => _materialSelectorService.SupportsTintColor;

    public string PackageVersion { get; set; } = RuntimeHelper.AppVersion;

    [ObservableProperty]
    public partial List<PlayerDisplayMode> PlayerDisplayModeCollection { get; set; }

    [ObservableProperty]
    public partial List<PreferCodecType> PreferCodecCollection { get; set; }

    [ObservableProperty]
    public partial List<PreferQualityType> PreferQualityCollection { get; set; }

    [ObservableProperty]
    public partial List<PlayerType> PlayerTypeCollection { get; set; }

    [ObservableProperty]
    public partial List<MTCBehavior> MTCBehaviorCollection { get; set; }

    public SettingsPageViewModel(
        ISettingsService settingsService,
        IWindowService windowService,
        INavigationService navigationService,
        IThemeSelectorService themeSelectorService,
        IMaterialSelectorService materialSelectorService
    )
    {
        Settings = settingsService.Settings;
        PlayerDisplayModeCollection = [.. Enum.GetValues<PlayerDisplayMode>()];
        PreferCodecCollection = [.. Enum.GetValues<PreferCodecType>()];
        PreferQualityCollection = [.. Enum.GetValues<PreferQualityType>()];
        PlayerTypeCollection = [.. Enum.GetValues<PlayerType>()];
        MTCBehaviorCollection = [.. Enum.GetValues<MTCBehavior>()];

        _windowService = windowService;
        _navigationService = navigationService;
        _themeSelectorService = themeSelectorService;
        _materialSelectorService = materialSelectorService;
        _materialSelectorService.AppearanceChanged += OnMaterialAppearanceChanged;
        Settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is null)
            {
                return;
            }

            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(AppSettings.AppTheme):
                    OnPropertyChanged(nameof(AppThemeText));
                    _themeSelectorService.SetTheme(Settings.AppTheme);
                    break;
                case nameof(AppSettings.IsTopNavBarShown):
                    OnPropertyChanged(nameof(IsTopNavShown));
                    _navigationService.IsTopNavBarShown = Settings.IsTopNavBarShown;
                    break;
            }
        };
    }

    private void OnMaterialAppearanceChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SelectedBackdropMaterial));
        OnPropertyChanged(nameof(IsBackdropFallbackEnabled));
        OnPropertyChanged(nameof(BackdropLuminosityOpacity));
        OnPropertyChanged(nameof(BackdropTintColor));
        OnPropertyChanged(nameof(SupportsBackdropFallback));
        OnPropertyChanged(nameof(SupportsBackdropLuminosityOpacity));
        OnPropertyChanged(nameof(SupportsBackdropTintColor));
    }

    [RelayCommand]
    private void ResetMaterial()
    {
        _materialSelectorService.ResetMaterial();
    }

    [RelayCommand]
    private async Task ChooseDownloadFolderAsync()
    {
        try
        {
            FolderPicker picker = new(_windowService.GetActiveWindowId() ?? default)
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
            };

            PickFolderResult? result = await picker.PickSingleFolderAsync();
            string? folderPath = result?.Path;
            if (folderPath is not null)
            {
                Settings.DownloadFolder = folderPath;
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task OpenDownloadFolderAsync()
    {
        if (!Directory.Exists(Settings.DownloadFolder))
        {
            Directory.CreateDirectory(Settings.DownloadFolder);
        }

        await Launcher.LaunchFolderPathAsync(Settings.DownloadFolder);
    }
}
