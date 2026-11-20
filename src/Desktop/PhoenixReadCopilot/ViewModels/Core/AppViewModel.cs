using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.BadgeNotifications;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Items;
using Windows.System;
using WinUIEx;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 应用视图模型.
/// </summary>
public sealed partial class AppViewModel : ViewModelBase
{
    private readonly AppSettings _settings;

    [ObservableProperty]
    public partial Window ActivatedWindow { get; set; }

    [ObservableProperty]
    public partial bool IsInitialLoading { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateShown { get; set; }

    /// <summary>
    /// 已创建的窗口列表.
    /// </summary>
    public List<Window> Windows { get; } = [];

    /// <summary>
    /// 是否已关闭.
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppViewModel"/> class.
    /// </summary>
    public AppViewModel(ISettingsService settingsService)
    {
        _settings = settingsService.Settings;
    }

    /// <summary>
    /// 重启应用程序.
    /// </summary>
    public static void Restart()
    {
        AppInstance.GetCurrent().UnregisterKey();
        AppInstance.Restart(null);
    }

    /// <summary>
    /// 进入主窗口并关闭启动窗口.
    /// </summary>
    public void EnterMainWindow()
    {
        _settings.ShouldSkipStartup = true;
        Window? startupWindow = Windows.FirstOrDefault(p => p is StartupWindow);
        if (Windows.FirstOrDefault(p => p is MainWindow) is { } mainWindow)
        {
            mainWindow.Activate();
        }
        else
        {
            IsInitialLoading = true;
            new MainWindow().Activate();
        }

        _ = ItemServices.Account.InitializeAsync();

        if (startupWindow is not null)
        {
            Windows.Remove(startupWindow);
            startupWindow.Close();
        }
    }

    /// <summary>
    /// 启动应用主逻辑.
    /// </summary>
    public void Launch()
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

    /// <summary>
    /// 更改所有窗口主题.
    /// </summary>
    public void ChangeTheme(ElementTheme theme)
    {
        foreach (Window window in Windows)
        {
            (window.Content as FrameworkElement).RequestedTheme = theme;
        }
    }

    public void MakeCurrentWindowEnterFullScreen()
    {
        Window window = ActivatedWindow;
        window.SetWindowPresenter(AppWindowPresenterKind.FullScreen);
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.EnterPlayerHostMode(PlayerDisplayMode.FullScreen);
        }
    }

    public void MakeCurrentWindowExitFullScreen()
    {
        Window window = ActivatedWindow;
        window.SetWindowPresenter(AppWindowPresenterKind.Default);
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }

    public void MakeCurrentWindowEnterOverlap()
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

    public void MakeCurrentWindowExitOverlap()
    {
        Window window = ActivatedWindow;
        window.SetWindowPresenter(AppWindowPresenterKind.Default);
        if (window is IPlayerHostWindow hostWindow)
        {
            hostWindow.ExitPlayerHostMode();
        }
    }

    public void MakeCurrentWindowEnterCompactOverlay()
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

    public void MakeCurrentWindowExitCompactOverlay()
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
    public async Task ShowTipAsync(string text, InfoType type = InfoType.Information)
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
                    await tipWindow.ShowTipAsync(text, type);
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

        await tipWindow.ShowTipAsync(text, type);
    }

    /// <summary>
    /// 显示提示 (同步触发).
    /// </summary>
    public void ShowTip(string text, InfoType type = InfoType.Information) =>
        _ = ShowTipAsync(text, type);

    /// <summary>
    /// 兼容 INotificationViewModel 接口的命令封装.
    /// </summary>
    public async Task ShowTipAsync((string, InfoType) data) =>
        await ShowTipAsync(data.Item1, data.Item2);

    public void HideUpdate() => IsUpdateShown = false;

    public async Task ShowUpdateAsync()
    {
        string packVersion = RuntimeHelper.AppVersion;
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
