using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.Storage.Pickers;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Extensions;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Core;
using Windows.System;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 设置页面视图模型.
/// </summary>
public sealed partial class SettingsPageViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsPageViewModel"/> class.
    /// </summary>
    public SettingsPageViewModel(
        ISettingsService settingsService,
        AppViewModel appViewModel,
        NavigationViewModel navigationViewModel,
        IAppToolkit appToolkit
    )
    {
        Settings = settingsService.Settings;
        _appViewModel = appViewModel;
        _navigationViewModel = navigationViewModel;
        _appToolkit = appToolkit;
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
                    _appViewModel.ChangeThemeCommand.Execute(Settings.AppTheme.ToElementTheme());
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
        PlayerTypeCollection = [.. Enum.GetValues<PlayerType>()];
        MTCBehaviorCollection = [.. Enum.GetValues<MTCBehavior>()];

        string copyrightTemplate = Resources.Copyright;
        Copyright = string.Format(copyrightTemplate, 2026);
        PackageVersion = _appToolkit.GetPackageVersion();

        _isInitialized = true;
    }

    [RelayCommand]
    private async Task ChooseDownloadFolderAsync()
    {
        try
        {
            FolderPicker picker = new(_appViewModel.ActivatedWindow.AppWindow.Id)
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
