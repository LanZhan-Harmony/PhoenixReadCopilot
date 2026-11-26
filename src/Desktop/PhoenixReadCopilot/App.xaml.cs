using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using WinUI3Package;
using ZLogger;

namespace PhoenixReadCopilot;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public sealed partial class App : Application
{
    private const string Id = "PhoenixReadCopilot";
    private readonly DispatcherQueue _dispatcherQueue;

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
        ServiceHelper.ConfigureServices();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        UnhandledException += OnUnhandledException;
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        AppInstance instance = AppInstance.FindOrRegisterForKey(Id);
        if (instance.IsCurrent)
        {
            instance.Activated += OnInstanceActivated;
            await Ioc.Default.GetRequiredService<IActivationService>().ActivateAsync(args);

            MainWindow? mainWindow = GetMainWindow();
            mainWindow?.Closed += OnMainWindowClosed;
        }
        else
        {
            AppActivationArguments activatedArgs = instance.GetActivatedEventArgs();
            _ = instance
                .RedirectActivationToAsync(activatedArgs)
                .AsTask()
                .ContinueWith(_ => Current.Exit());
        }

        CursorController.ApplyCursorWorkaround();
    }

    /// <summary>
    /// 获取当前应用主窗口.
    /// </summary>
    internal static MainWindow? GetMainWindow() =>
        Ioc.Default.GetRequiredService<IWindowService>().GetWindow<MainWindow>();

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        ExitApp();
    }

    private void OnInstanceActivated(object? sender, AppActivationArguments e)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            await Ioc.Default.GetRequiredService<IActivationService>().ActivateAsync(e);
        });
    }

    private void ExitApp()
    {
        Exit();
    }

    private void OnUnhandledException(
        object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e
    )
    {
        Ioc.Default.GetRequiredService<ILogger<App>>()
            .ZLogError(e.Exception, $"Unhandled exception occurred.");
        e.Handled = true;

        // 一旦出现布局循环检测异常，就删除上次选中的功能页，然后重启应用.
        if (e.Message.Contains("Layout cycle detected"))
        {
            Ioc.Default.GetRequiredService<ISettingsService>().Settings.LastSelectedFeaturePage =
                string.Empty;
            Ioc.Default.GetRequiredService<IAppLifecycleService>().Restart();
        }
    }
}