using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 页面定位服务实现（基于字典静态映射，不使用反射）.
/// </summary>
internal sealed class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = [with(StringComparer.OrdinalIgnoreCase)];

    public PageService()
    {
        // 顶级主页面配置
        Configure<HomePageViewModel, HomePage>();
        Configure<RankPageViewModel, RankPage>();
        Configure<CalendarPageViewModel, CalendarPage>();
        Configure<FilterPageViewModel, FilterPage>();
        Configure<SettingsPageViewModel, SettingsPage>();
        Configure<GalleryPageViewModel, GalleryPage>();
        Configure<StartupPageViewModel, StartupPage>();
        Configure<UserSpacePageViewModel, UserSpacePage>();

        // 浮层/详情页面配置
        Configure<VideoPlayerPageViewModel, VideoPlayerPage>();
        Configure<SearchPageViewModel, SearchPage>();
        Configure<ArticlePageViewModel, ArticlePage>();
    }

    /// <inheritdoc/>
    public Type GetPageType(string key)
    {
        lock (_pages)
        {
            if (!_pages.TryGetValue(key, out Type? pageType))
            {
                throw new ArgumentException(
                    $"Page not found: {key}. Did you forget to call PageService.Configure?"
                );
            }

            return pageType;
        }
    }

    private void Configure<TVm, TView>()
        where TVm : class
        where TView : Page
    {
        lock (_pages)
        {
            Type viewType = typeof(TView);
            Type vmType = typeof(TVm);

            // 注册 ViewModel 全名与类名
            _pages[vmType.FullName!] = viewType;
            _pages[vmType.Name] = viewType;

            // 注册 View 全名与类名
            _pages[viewType.FullName!] = viewType;
            _pages[viewType.Name] = viewType;
        }
    }
}
