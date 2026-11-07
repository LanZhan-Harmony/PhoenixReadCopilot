using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.BadgeNotifications;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Services;
using Richasy.BiliKernel.Bili.Authorization;
using Windows.System;
using WinUIEx;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 应用视图模型.
/// </summary>
public sealed partial class AppViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppViewModel"/> class.
    /// </summary>
    public AppViewModel(
        ILogger<AppViewModel> logger,
        IBiliTokenResolver tokenResolver,
        ISettingsService settingsService,
        IAppToolkit appToolkit
    )
    {
        _logger = logger;
        _tokenResolver = tokenResolver;
        _settings = settingsService.Settings;
        _appToolkit = appToolkit;
    }

    [RelayCommand]
    private static void Restart()
    {
        Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().UnregisterKey();
        Microsoft.Windows.AppLifecycle.AppInstance.Restart(default);
    }

    [RelayCommand]
    private Task InitializeExternalAsync()
    {
        var architecture = RuntimeInformation.ProcessArchitecture;
        var identifier = architecture == Architecture.Arm64 ? "arm64" : "x64";

        FFmpegPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "ffmpeg",
            identifier,
            "ffmpeg.exe"
        );
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Launch()
    {
        // 清除所有之前的badges（仅在 MSIX 打包环境下支持）.
        if (RuntimeHelper.IsMSIX)
        {
            try
            {
                BadgeNotificationManager.Current.ClearBadge();
            }
            catch { }
        }

        var hasToken = false;
        try
        {
            hasToken = _tokenResolver.GetToken() is not null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get token during launch.");
        }

        if (hasToken)
        {
            IsInitialLoading = true;
            new MainWindow().Activate();
            InitializeExternalCommand.Execute(default);
        }
        else
        {
            new StartupWindow().Activate();
        }
    }

    [RelayCommand]
    private void ChangeTheme(ElementTheme theme)
    {
        foreach (var window in Windows)
        {
            (window.Content as FrameworkElement).RequestedTheme = theme;
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowEnterFullScreen()
    {
        var window = ActivatedWindow;
        window.SetWindowPresenter(AppWindowPresenterKind.FullScreen);
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.EnterPlayerHostMode(PlayerDisplayMode.FullScreen);
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowExitFullScreen()
    {
        var window = ActivatedWindow;
        window.SetWindowPresenter(AppWindowPresenterKind.Default);
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowEnterOverlap()
    {
        var window = ActivatedWindow;
        if (window.AppWindow.Presenter is not OverlappedPresenter)
        {
            window.SetWindowPresenter(AppWindowPresenterKind.Overlapped);
        }

        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.EnterPlayerHostMode(PlayerDisplayMode.FullWindow);
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowExitOverlap()
    {
        var window = ActivatedWindow;
        window.SetWindowPresenter(AppWindowPresenterKind.Default);
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowEnterCompactOverlay()
    {
        var window = ActivatedWindow;
        (window as WinUIEx.WindowEx).MinHeight = 320;
        (window as WinUIEx.WindowEx).MinWidth = 560;
        window.SetWindowPresenter(AppWindowPresenterKind.CompactOverlay);
        window.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.EnterPlayerHostMode(PlayerDisplayMode.CompactOverlay);
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowExitCompactOverlay()
    {
        var window = ActivatedWindow;
        (window as WinUIEx.WindowEx).MinHeight = 480;
        (window as WinUIEx.WindowEx).MinWidth = 640;
        window.SetWindowPresenter(AppWindowPresenterKind.Default);
        window.AppWindow.TitleBar.PreferredHeightOption =
            window is MainWindow ? TitleBarHeightOption.Tall : TitleBarHeightOption.Standard;
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }

    /// <summary>
    /// 显示提示.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [RelayCommand]
    private async Task ShowTipAsync((string, InfoType) data)
    {
        if (ActivatedWindow is ITipWindow tipWindow)
        {
            await tipWindow.ShowTipAsync(data.Item1, data.Item2);
        }
        else
        {
            var firstWindow = Windows.OfType<ITipWindow>().FirstOrDefault();
            if (firstWindow is not null)
            {
                await firstWindow.ShowTipAsync(data.Item1, data.Item2);
            }
        }
    }

    [RelayCommand]
    private void CheckUpate()
    {
        var localVersion = _settings.AppVersion;
        var currentVersion = _appToolkit.GetPackageVersion();
        if (localVersion != currentVersion)
        {
            _settings.AppVersion = currentVersion;
            IsUpdateShown = true;
        }
    }

    [RelayCommand]
    private void HideUpdate() => IsUpdateShown = false;

    [RelayCommand]
    private async Task ShowUpdateAsync()
    {
        var packVersion = _appToolkit.GetPackageVersion();
        var url = $"https://github.com/Richasy/Bili.Copilot/releases/tag/v{packVersion}";
        await Launcher.LaunchUriAsync(new Uri(url));
        HideUpdate();
    }

    partial void OnIsInitialLoadingChanged(bool value)
    {
        if (!value)
        {
            // Started via NotificationViewModel
        }
    }
}
