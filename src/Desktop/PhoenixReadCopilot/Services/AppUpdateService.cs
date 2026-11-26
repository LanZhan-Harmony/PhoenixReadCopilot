using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Updater;
using ZLogger;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 应用更新服务，负责检查 GitHub Release 更新并在发现新版本时弹窗提示用户.
/// 仅在非 MSIX 打包环境下工作；MSIX 环境下由 Microsoft Store 自动更新.
/// </summary>
internal sealed class AppUpdateService(
    ILogger<AppUpdateService> logger,
    DispatcherQueue dispatcherQueue
)
{
    private readonly ILogger<AppUpdateService> _logger = logger;
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly GitHubUpdater _updater = new();
    private AppRelease? _pendingRelease;

    /// <summary>
    /// 在应用启动时调用，异步检查更新并在发现新版本时弹出提示.
    /// MSIX 打包环境下直接跳过.
    /// </summary>
    public async Task CheckForUpdateOnStartupAsync()
    {
        // MSIX 打包环境下由 Store 自动更新，不走此逻辑
        if (RuntimeHelper.IsMSIX)
        {
            return;
        }

        try
        {
            Version currentVersion = GetCurrentVersion();
            AppRelease? release = await _updater.CheckForUpdateAsync(currentVersion);
            if (release is null)
            {
                return;
            }

            _pendingRelease = release;

            // 切回 UI 线程弹窗
            _dispatcherQueue.TryEnqueue(async () =>
            {
                await ShowUpdateDialogAsync(release);
            });
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"检查更新失败.");
        }
    }

    /// <summary>
    /// 弹出更新提示对话框.
    /// </summary>
    private async Task ShowUpdateDialogAsync(AppRelease release)
    {
        try
        {
            Window? mainWindow = App.GetMainWindow();
            if (mainWindow?.Content is not FrameworkElement rootElement)
            {
                return;
            }

            string preReleaseText = release.IsPrerelease ? " (预发布版)" : "";
            ContentDialog dialog = new()
            {
                Title = "发现新版本",
                Content =
                    $"发现新版本 {release.Version}{preReleaseText}，当前版本 {GetCurrentVersion()}。\n\n"
                    + "是否立即下载并更新？更新将在下载完成后自动重启应用。",
                PrimaryButtonText = "立即更新",
                SecondaryButtonText = "查看更新",
                CloseButtonText = "稍后再说",
                DefaultButton = ContentDialogButton.Primary,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                XamlRoot = rootElement.XamlRoot,
            };

            ContentDialogResult result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await StartUpdateAsync(release, rootElement);
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // 打开 GitHub Release 页面
                string url =
                    $"https://github.com/LanZhan-Harmony/PhoenixReadCopilot/releases/tag/{release.Asset.TagName}";
                await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"显示更新对话框失败.");
        }
    }

    /// <summary>
    /// 下载并应用更新.
    /// </summary>
    private async Task StartUpdateAsync(AppRelease release, FrameworkElement rootElement)
    {
        // 创建进度对话框
        ProgressBar progressBar = new()
        {
            IsIndeterminate = false,
            Minimum = 0,
            Maximum = 100,
            Width = 300,
        };

        TextBlock statusText = new()
        {
            Text = "正在选择最快的下载节点...",
            Margin = new Thickness(0, 8, 0, 0),
        };

        StackPanel panel = new() { Spacing = 4, Children = { progressBar, statusText } };

        ContentDialog progressDialog = new()
        {
            Title = $"正在更新到 {release.Version}",
            Content = panel,
            CloseButtonText = "取消",
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            XamlRoot = rootElement.XamlRoot,
        };

        CancellationTokenSource cts = new();
        progressDialog.CloseButtonClick += (_, _) =>
        {
            cts.Cancel();
        };

        // 在后台线程执行下载
        Task<UpdateResult> updateTask = Task.Run(
            async () =>
            {
                string targetDir = AppContext.BaseDirectory;
                Progress<(long Downloaded, long Total)> progress = new(p =>
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        if (p.Total > 0)
                        {
                            double percent = (double)p.Downloaded / p.Total * 100;
                            progressBar.Value = percent;
                            statusText.Text =
                                $"正在下载... {p.Downloaded / 1024 / 1024}MB / {p.Total / 1024 / 1024}MB ({percent:F0}%)";
                        }
                        else
                        {
                            progressBar.IsIndeterminate = true;
                            statusText.Text = $"正在下载... {p.Downloaded / 1024 / 1024}MB";
                        }
                    });
                });

                return await UpdateApplier.DownloadAndStageAsync(
                    _updater,
                    release,
                    targetDir,
                    progress,
                    ct: cts.Token
                );
            },
            cts.Token
        );

        // 显示进度对话框（非阻塞地等待更新完成）
        _ = progressDialog.ShowAsync();

        UpdateResult result;
        try
        {
            result = await updateTask;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"更新下载失败.");
            result = new UpdateResult(UpdateOutcome.Failed, release.Version, ex.Message);
        }

        progressDialog.Hide();

        if (result.Outcome == UpdateOutcome.Staged)
        {
            // 更新已准备就绪，退出应用让新版本替换
            _logger.ZLogInformation($"更新已准备就绪，退出应用进行替换.");
            Application.Current.Exit();
        }
        else if (result.Outcome == UpdateOutcome.Failed)
        {
            ContentDialog failDialog = new()
            {
                Title = "更新失败",
                Content = $"更新失败: {result.Message}\n\n您可以稍后重试或手动从 GitHub 下载更新。",
                CloseButtonText = "确定",
                XamlRoot = rootElement.XamlRoot,
            };
            await failDialog.ShowAsync();
        }
    }

    private static Version GetCurrentVersion()
    {
        string versionStr = RuntimeHelper.AppVersion;
        return Version.TryParse(versionStr, out Version? version)
            ? version
            : new Version(0, 0, 0, 0);
    }
}