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
using Richasy.BiliKernel.Authenticator;
using Richasy.BiliKernel.Authorizers.TV;
using Richasy.BiliKernel.Bili.Authorization;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Bili.Moment;
using Richasy.BiliKernel.Bili.Search;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Http;
using Richasy.BiliKernel.Resolvers.WinUIQRCode;
using Richasy.BiliKernel.Services.Media;
using Richasy.BiliKernel.Services.Moment;
using Richasy.BiliKernel.Services.Search;
using Richasy.BiliKernel.Services.User;
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
            // 4. 红果客户端单例 (双轨并存)
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
            // 5. Bili 客户端
            .AddSingleton<BiliHttpClient>()
            .AddSingleton<BiliAuthenticator>()
            .AddSingleton(
                (Func<IServiceProvider, IQRCodeResolver>)(
                    _ => new WinUIQRCodeResolver(RenderQRCodeAsync)
                )
            )
            .AddSingleton<IAuthenticationService, TVAuthenticationService>()
            .AddSingleton<IMyProfileService, MyProfileService>()
            .AddSingleton<IRelationshipService, RelationshipService>()
            .AddSingleton<IViewLaterService, ViewLaterService>()
            .AddSingleton<IViewHistoryService, ViewHistoryService>()
            .AddSingleton<IMessageService, MessageService>()
            .AddSingleton<IFavoriteService, FavoriteService>()
            .AddSingleton<IUserService, UserService>()
            .AddSingleton<IVideoDiscoveryService, VideoDiscoveryService>()
            .AddSingleton<IEntertainmentDiscoveryService, EntertainmentDiscoveryService>()
            .AddSingleton<IMomentDiscoveryService, MomentDiscoveryService>()
            .AddSingleton<IMomentOperationService, MomentOperationService>()
            .AddSingleton<IPlayerService, PlayerService>()
            .AddSingleton<ISubtitleService, SubtitleService>()
            .AddSingleton<ISearchService, SearchService>()
            // 5. 本地凭证解析器
            .AddSingleton<IBiliCookiesResolver, LocalBiliCookiesResolver>()
            .AddSingleton<IBiliTokenResolver, LocalBiliTokenResolver>()
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
            .AddSingleton<MessagePageViewModel>()
            .AddTransient<MomentPageViewModel>()
            .AddTransient<PopularPageViewModel>()
            .AddTransient<SearchPageViewModel>()
            .AddSingleton<SettingsPageViewModel>()
            .AddSingleton<StartupPageViewModel>()
            .AddTransient<UserSpacePageViewModel>()
            .AddTransient<VideoPartitionPageViewModel>()
            .AddTransient<VideoPlayerPageViewModel>()
            .AddTransient<ViewLaterPageViewModel>()
            // Components
            .AddTransient<CommentMainViewModel>()
            .BuildServiceProvider();

        Ioc.Default.ConfigureServices(serviceProvider);
    }

    private static Task RenderQRCodeAsync(byte[] imageData)
    {
        StartupPageViewModel vm = Ioc.Default.GetRequiredService<StartupPageViewModel>();
        vm.RenderQRCodeCommand.Execute(imageData);
        return Task.CompletedTask;
    }
}
