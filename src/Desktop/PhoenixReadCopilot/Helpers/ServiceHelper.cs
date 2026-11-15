using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Extensions;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share;
using WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.Helpers;

internal static class ServiceHelper
{
    /// <summary>
    /// 配置依赖注入容器服务.
    /// </summary>
    public static void ConfigureServices()
    {
        IServiceCollection services = new ServiceCollection()
            // 1. 通用工具
            .AddSingleton<IPathService, PathService>()
            .AddSingleton<ILocalizationService, LocalizationService>()
            .AddSingleton<ILocalViewLaterService, LocalViewLaterService>()
            .AddSingleton<IFontToolkit, SharedFontToolkit>()
            .AddSingleton<IResourceToolkit, SharedResourceToolkit>()
            .AddSingleton<IXamlRootProvider, XamlRootProvider>()
            .AddSingleton(DispatcherQueue.GetForCurrentThread())
            .AddSingleton<ISubtitleService, SubtitleService>()
            .AddSingleton<INotificationService, NotificationService>()
            // 2. ViewModels
            // Core
            .AddSingleton<AppViewModel>()
            .AddTransient<DanmakuViewModel>()
            .AddTransient<DramaReviewSectionDetailViewModel>()
            .AddTransient<DownloadViewModel>()
            .AddTransient<FFmpegPlayerViewModel>()
            .AddSingleton<NavigationViewModel>()
            .AddSingleton<PinnerViewModel>()
            .AddTransient<SubtitleViewModel>()
            .AddSingleton<EmoteModuleViewModel>()
            .AddSingleton<AccountViewModel>()
            .AddSingleton<SearchBoxViewModel>()
            // View
            .AddTransient<HomePageViewModel>()
            .AddTransient<CalendarPageViewModel>()
            .AddTransient<FilterPageViewModel>()
            .AddTransient<FansPageViewModel>()
            .AddTransient<FavoritesPageViewModel>()
            .AddTransient<FollowsPageViewModel>()
            .AddTransient<GalleryPageViewModel>()
            .AddTransient<HistoryPageViewModel>()
            .AddTransient<MomentPageViewModel>()
            .AddTransient<RankPageViewModel>()
            .AddTransient<SearchPageViewModel>()
            .AddSingleton<SettingsPageViewModel>()
            .AddSingleton<StartupPageViewModel>()
            .AddTransient<UserSpacePageViewModel>()
            .AddTransient<VideoPlayerPageViewModel>()
            .AddTransient<ViewLaterPageViewModel>()
            // Components
            .AddTransient<CommentMainViewModel>();

        Core.Helpers.ServiceHelper.ConfigureCoreServices(services);
        ServiceProvider provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);
    }
}
