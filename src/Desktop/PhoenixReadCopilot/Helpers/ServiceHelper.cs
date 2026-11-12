using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Extensions;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Toolkits;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.View;
using Serilog;
using WinUIKernel.Share;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.Helpers;

internal class ServiceHelper
{
    /// <summary>
    /// 配置依赖注入容器服务.
    /// </summary>
    public static void ConfigureServices()
    {
        string loggerPath = PathHelper.LogFolderPath;
        Directory.CreateDirectory(loggerPath);
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(loggerPath, $"log-{DateTimeOffset.Now:yyyy-MM-dd}.txt"))
            .CreateLogger();
        ServiceProvider serviceProvider = new ServiceCollection()
            // 1. Serilog 日志
            .AddLogging(b => b.AddSerilog(dispose: true))
            // 2. UI 线程调度
            .AddSingleton(DispatcherQueue.GetForCurrentThread())
            // 3. 通用工具与 XamlRootProvider
            .AddSingleton<ISettingsService, SettingsService>()
            .AddSingleton<ILocalViewLaterService, LocalViewLaterService>()
            .AddSingleton<IAppToolkit, AppToolkit>()
            .AddSingleton<IFileToolkit, FileToolkit>()
            .AddSingleton<IFontToolkit, SharedFontToolkit>()
            .AddSingleton<IResourceToolkit, SharedResourceToolkit>()
            .AddSingleton<IXamlRootProvider, XamlRootProvider>()
            // 4. 红果客户端单例
            .AddSingleton<PhoenixReadApiClient>(_ =>
            {
                string sessionPath = Path.Combine(
                    PathHelper.AuthFolderPath,
                    "phoenix_session.json"
                );
                return new PhoenixReadApiClient(
                    new PhoenixReadApiOptions
                    {
                        AuthenticationStateFilePath = sessionPath,
                        RequestTimeout = TimeSpan.FromSeconds(25),
                        RetryCount = 2,
                    }
                );
            })
            // 5. 辅助服务
            .AddSingleton<ISubtitleService, SubtitleService>()
            // 6. ViewModels
            // Core
            .AddSingleton<AppViewModel>()
            .AddTransient<DanmakuViewModel>()
            .AddTransient<DramaReviewSectionDetailViewModel>()
            .AddTransient<DownloadViewModel>()
            .AddTransient<FFmpegPlayerViewModel>()
            .AddSingleton<NotificationViewModel>()
            .AddSingleton<INotificationViewModel>(sp =>
                sp.GetRequiredService<NotificationViewModel>()
            )
            .AddSingleton<NavigationViewModel>()
            .AddSingleton<PinnerViewModel>()
            .AddTransient<SubtitleViewModel>()
            .AddSingleton<EmoteModuleViewModel>()
            .AddSingleton<AccountViewModel>()
            .AddSingleton<SearchBoxViewModel>()
            // View
            .AddTransient<CalendarPageViewModel>()
            .AddTransient<FindDramaPageViewModel>()
            .AddTransient<FansPageViewModel>()
            .AddTransient<FavoritesPageViewModel>()
            .AddTransient<FollowsPageViewModel>()
            .AddTransient<GalleryPageViewModel>()
            .AddTransient<HistoryPageViewModel>()
            .AddTransient<MomentPageViewModel>()
            .AddTransient<PopularPageViewModel>()
            .AddTransient<SearchPageViewModel>()
            .AddSingleton<SettingsPageViewModel>()
            .AddSingleton<StartupPageViewModel>()
            .AddTransient<UserSpacePageViewModel>()
            .AddTransient<VideoPlayerPageViewModel>()
            .AddTransient<ViewLaterPageViewModel>()
            // Components
            .AddTransient<CommentMainViewModel>()
            .BuildServiceProvider();

        Ioc.Default.ConfigureServices(serviceProvider);
    }
}
