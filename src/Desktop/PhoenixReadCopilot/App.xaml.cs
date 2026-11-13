using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Core;
using ZLogger;

namespace PhoenixReadCopilot;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private const string Id = "PhoenixReadCopilot";
    private readonly DispatcherQueue _dispatcherQueue;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
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
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppInstance instance = AppInstance.FindOrRegisterForKey(Id);
        if (instance.IsCurrent)
        {
            instance.Activated += OnInstanceActivated;
            Ioc.Default.GetRequiredService<AppViewModel>().LaunchCommand.Execute(null);
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
    }

    private static MainWindow? GetMainWindow() =>
        Ioc.Default.GetRequiredService<AppViewModel>().Windows.Find(p => p is MainWindow)
        as MainWindow;

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        ExitApp();
    }

    private void OnInstanceActivated(object? sender, AppActivationArguments e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            GetMainWindow()?.Activate();
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
            Ioc.Default.GetRequiredService<AppViewModel>().RestartCommand.Execute(null);
        }
    }
}
