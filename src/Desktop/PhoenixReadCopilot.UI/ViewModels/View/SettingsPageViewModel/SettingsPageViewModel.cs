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
    private readonly AppViewModel _appViewModel;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly IAppToolkit _appToolkit;
    private readonly IFileToolkit _fileToolkit;
    public AppSettings Settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageViewModel"/> class.
    /// </summary>
    public SettingsPageViewModel(
        ISettingsService settingsService,
        AppViewModel appViewModel,
        NavigationViewModel navigationViewModel,
        IAppToolkit appToolkit,
        IFileToolkit fileToolkit
    )
    {
        Settings = settingsService.Settings;
        _appViewModel = appViewModel;
        _navigationViewModel = navigationViewModel;
        _appToolkit = appToolkit;
        _fileToolkit = fileToolkit;
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
                    _appViewModel.ChangeThemeCommand.Execute(Settings.AppTheme);
                    break;
                case nameof(AppSettings.IsTopNavBarShown):
                    OnPropertyChanged(nameof(IsTopNavShown));
                    _navigationViewModel.IsTopNavBarShown = Settings.IsTopNavBarShown;
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
        PackageVersion = _appToolkit.GetPackageVersion();

        _isInitialized = true;
    }

    [RelayCommand]
    private async Task ChooseDownloadFolderAsync()
    {
        var folderPath = await _fileToolkit.PickFolderAsync(
            _appViewModel.ActivatedWindow.AppWindow.Id
        );
        if (folderPath != null)
        {
            Settings.DownloadFolder = folderPath;
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
