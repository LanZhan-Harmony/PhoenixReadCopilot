using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.Storage.Pickers;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Extensions;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Strings;
using Windows.System;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 设置页面视图模型.
/// </summary>
internal sealed partial class SettingsPageViewModel : ViewModelBase
{
    private readonly IWindowService _windowService;
    private readonly INavigationService _navigationService;
    public AppSettings Settings;
    private bool _isInitialized;

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

    [ObservableProperty]
    public partial string PackageVersion { get; set; }

    [ObservableProperty]
    public partial string Copyright { get; set; }

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
        INavigationService navigationService
    )
    {
        Settings = settingsService.Settings;
        _windowService = windowService;
        _navigationService = navigationService;
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
                    _windowService.ChangeTheme(Settings.AppTheme.ToElementTheme());
                    break;
                case nameof(AppSettings.IsTopNavBarShown):
                    OnPropertyChanged(nameof(IsTopNavShown));
                    _navigationService.IsTopNavBarShown = Settings.IsTopNavBarShown;
                    break;
            }
        };
    }

    [RelayCommand]
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        PlayerDisplayModeCollection = [.. Enum.GetValues<PlayerDisplayMode>()];
        PreferCodecCollection = [.. Enum.GetValues<PreferCodecType>()];
        PreferQualityCollection = [.. Enum.GetValues<PreferQualityType>()];
        PlayerTypeCollection = [.. Enum.GetValues<PlayerType>()];
        MTCBehaviorCollection = [.. Enum.GetValues<MTCBehavior>()];

        string copyrightTemplate = Resources.Copyright;
        Copyright = string.Format(copyrightTemplate, 2026);
        PackageVersion = RuntimeHelper.AppVersion;

        _isInitialized = true;
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
