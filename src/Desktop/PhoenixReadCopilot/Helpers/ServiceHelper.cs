using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Extensions;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Toolkits;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.View;
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
        IServiceCollection services = new ServiceCollection()
            // 1. 通用工具
            .AddSingleton<IPathService, PathService>()
            .AddSingleton<ILocalViewLaterService, LocalViewLaterService>()
            .AddSingleton<IAppToolkit, AppToolkit>()
            .AddSingleton<IFontToolkit, SharedFontToolkit>()
            .AddSingleton<IResourceToolkit, SharedResourceToolkit>()
            .AddSingleton<IXamlRootProvider, XamlRootProvider>()
            .AddSingleton(DispatcherQueue.GetForCurrentThread())
            .AddSingleton<ISubtitleService, SubtitleService>()
            // 2. ViewModels
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
            .AddTransient<CommentMainViewModel>();

        Core.Helpers.ServiceHelper.ConfigureCoreServices(services);
        ServiceProvider provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);
    }
}
