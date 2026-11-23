using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Activation;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Helpers;

internal static class ServiceHelper
{
    /// <summary>
    /// 配置依赖注入容器服务.
    /// </summary>
    public static void ConfigureServices()
    {
        IServiceCollection services = new ServiceCollection()
            // 0. 生命周期与激活服务
            .AddTransient<IActivationHandler, NotificationActivationHandler>()
            .AddTransient<IActivationHandler, DefaultActivationHandler>()
            .AddSingleton<IActivationService, ActivationService>()
            .AddSingleton<IAppLifecycleService, AppLifecycleService>()
            .AddSingleton<IWindowService, WindowService>()
            // 1. 通用工具
            .AddSingleton<IPathService, PathService>()
            .AddSingleton<ILocalizationService, LocalizationService>()
            .AddSingleton(DispatcherQueue.GetForCurrentThread())
            .AddSingleton<INotificationService, NotificationService>()
            .AddSingleton<IPageService, PageService>()
            .AddSingleton<NavigationService>()
            .AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>())
            .AddSingleton<INavigationViewService, NavigationViewService>()
            .AddSingleton<AppUpdateService>()
            // 2. ViewModels
            // Core
            .AddTransient<DanmakuViewModel>()
            .AddTransient<DramaReviewSectionDetailViewModel>()
            .AddTransient<FFmpegPlayerViewModel>()
            .AddSingleton<RootLayoutViewModel>()
            .AddSingleton<PinnerViewModel>()
            .AddTransient<SubtitleViewModel>()
            .AddSingleton<EmoteModuleViewModel>()
            .AddSingleton<AccountViewModel>()
            .AddSingleton<SearchBoxViewModel>()
            // View
            .AddTransient<HomePageViewModel>()
            .AddTransient<CalendarPageViewModel>()
            .AddTransient<FilterPageViewModel>()
            .AddTransient<GalleryPageViewModel>()
            .AddTransient<RankPageViewModel>()
            .AddTransient<SearchPageViewModel>()
            .AddSingleton<SettingsPageViewModel>()
            .AddSingleton<StartupPageViewModel>()
            .AddTransient<UserSpacePageViewModel>()
            .AddTransient<ArticlePageViewModel>()
            .AddTransient<VideoPlayerPageViewModel>()
            // Components
            .AddTransient<CommentMainViewModel>();

        Core.Helpers.ServiceHelper.ConfigureCoreServices(services);
        ServiceProvider provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);
    }
}
