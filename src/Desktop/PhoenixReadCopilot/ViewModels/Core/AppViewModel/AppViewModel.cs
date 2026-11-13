using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.BadgeNotifications;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Items;
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
    public AppViewModel(ISettingsService settingsService, IAppToolkit appToolkit)
    {
        _settings = settingsService.Settings;
        _appToolkit = appToolkit;
    }

    [RelayCommand]
    private static void Restart()
    {
        Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().UnregisterKey();
        Microsoft.Windows.AppLifecycle.AppInstance.Restart(null);
    }

    /// <summary>
    /// 进入主窗口并关闭启动窗口.
    /// </summary>
    [RelayCommand]
    public void EnterMainWindow()
    {
        _settings.ShouldSkipStartup = true;
        Window? startupWindow = Windows.FirstOrDefault(p => p is StartupWindow);
        if (Windows.FirstOrDefault(p => p is MainWindow) is { } mainWindow)
        {
            mainWindow.Activate();
            ItemServices.Account.InitializeCommand.Execute(null);
        }
        else
        {
            IsInitialLoading = true;
            new MainWindow().Activate();
            InitializeExternalCommand.Execute(null);
        }

        if (startupWindow is not null)
        {
            Windows.Remove(startupWindow);
            startupWindow.Close();
        }
    }

    [RelayCommand]
    private Task InitializeExternalAsync()
    {
        Architecture architecture = RuntimeInformation.ProcessArchitecture;
        string identifier = architecture == Architecture.Arm64 ? "arm64" : "x64";

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

        EnterMainWindow();
    }

    [RelayCommand]
    private void ChangeTheme(ElementTheme theme)
    {
        foreach (Window window in Windows)
        {
            (window.Content as FrameworkElement).RequestedTheme = theme;
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowEnterFullScreen()
    {
        Window window = ActivatedWindow;
        window.SetWindowPresenter(AppWindowPresenterKind.FullScreen);
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.EnterPlayerHostMode(PlayerDisplayMode.FullScreen);
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowExitFullScreen()
    {
        Window window = ActivatedWindow;
        window.SetWindowPresenter(AppWindowPresenterKind.Default);
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowEnterOverlap()
    {
        Window window = ActivatedWindow;
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
        Window window = ActivatedWindow;
        window.SetWindowPresenter(AppWindowPresenterKind.Default);
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }

    [RelayCommand]
    private void MakeCurrentWindowEnterCompactOverlay()
    {
        Window window = ActivatedWindow;
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
        Window window = ActivatedWindow;
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
        ITipWindow? tipWindow =
            ActivatedWindow as ITipWindow ?? Windows.OfType<ITipWindow>().FirstOrDefault();
        if (tipWindow is null)
        {
            return;
        }

        if (tipWindow is Window window && !window.DispatcherQueue.HasThreadAccess)
        {
            TaskCompletionSource tcs = new();
            window.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await tipWindow.ShowTipAsync(data.Item1, data.Item2);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            await tcs.Task;
            return;
        }

        await tipWindow.ShowTipAsync(data.Item1, data.Item2);
    }

    [RelayCommand]
    private void CheckUpate()
    {
        string localVersion = _settings.AppVersion;
        string currentVersion = _appToolkit.GetPackageVersion();
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
        string packVersion = _appToolkit.GetPackageVersion();
        string url =
            $"https://github.com/LanZhan-Harmony/PhoenixReadCopilot/releases/tag/v{packVersion}";
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
