using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Models.Settings;
using PhoenixReadCopilot.UI.Services;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.ViewModels.Core;
using Windows.System;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 设置页面视图模型.
/// </summary>
public sealed partial class SettingsPageViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageViewModel"/> class.
    /// </summary>
    public SettingsPageViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        Settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == null)
            {
                return;
            }

            OnPropertyChanged(e.PropertyName);
            switch (e.PropertyName)
            {
                case nameof(AppSettings.AppTheme):
                    OnPropertyChanged(nameof(AppThemeText));
                    this.Get<AppViewModel>()
                        .ChangeThemeCommand.Execute(_settingsService.Settings.AppTheme);
                    break;
                case nameof(AppSettings.IsTopNavBarShown):
                    OnPropertyChanged(nameof(IsTopNavShown));
                    this.Get<NavigationViewModel>().IsTopNavBarShown = _settingsService
                        .Settings
                        .IsTopNavBarShown;
                    break;
            }
        };
    }

    [RelayCommand]
    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        PlayerDisplayModeCollection = [.. Enum.GetValues<PlayerDisplayMode>()];
        PreferCodecCollection = [.. Enum.GetValues<PreferCodecType>()];
        PreferQualityCollection = [.. Enum.GetValues<PreferQualityType>()];
        PreferDecodeCollection = [.. Enum.GetValues<PreferDecodeType>()];
        PlayerTypeCollection = [.. Enum.GetValues<PlayerType>()];
        MTCBehaviorCollection = [.. Enum.GetValues<MTCBehavior>()];

        var copyrightTemplate = Resources.Copyright;
        Copyright = string.Format(copyrightTemplate, 2026);
        PackageVersion = this.Get<IAppToolkit>().GetPackageVersion();

        _isInitialized = true;
    }

    [RelayCommand]
    private async Task ChooseDownloadFolderAsync()
    {
        var folder = await this.Get<IFileToolkit>()
            .PickFolderAsync(this.Get<AppViewModel>().ActivatedWindow);
        if (folder != null)
        {
            Settings.DownloadFolder = folder.Path;
        }
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
