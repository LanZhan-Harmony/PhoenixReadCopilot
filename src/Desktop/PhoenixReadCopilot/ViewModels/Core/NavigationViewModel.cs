using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 导航视图模型.
/// </summary>
public sealed partial class NavigationViewModel : ViewModelBase, INavServiceViewModel
{
    private readonly AppViewModel _appViewModel;
    private readonly AppSettings _settings;

    /// <summary>
    /// 覆盖层导航事件.
    /// </summary>
    public event EventHandler<Type>? OverlayNavigated;

    public NavigationViewModel(ISettingsService settingsService, AppViewModel appViewModel)
    {
        _appViewModel = appViewModel;
        _settings = settingsService.Settings;
    }

    private Frame? _navFrame;
    private Frame? _overFrame;

    [ObservableProperty]
    public partial bool IsOverlayOpen { get; set; }

    /// <summary>
    /// 导航条目列表.
    /// </summary>
    [ObservableProperty]
    public partial List<AppNavigationItemViewModel> MenuItems { get; set; }

    [ObservableProperty]
    public partial bool IsTopNavBarShown { get; set; }

    [ObservableProperty]
    public partial bool IsFavoritesPage { get; set; }

    [ObservableProperty]
    public partial bool IsHistoryPage { get; set; }

    [ObservableProperty]
    public partial bool IsViewLaterPage { get; set; }

    /// <summary>
    /// 底部条目列表.
    /// </summary>
    public ObservableCollection<AppNavigationItemViewModel> FooterItems { get; } = [];

    /// <inheritdoc/>
    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (_navFrame is null)
        {
            throw new InvalidOperationException("导航框架未初始化.");
        }

        ActiveMainWindow();
        string lastSelectedPage = _settings.LastSelectedFeaturePage;
        if (IsOverlayOpen)
        {
            IsOverlayOpen = false;
            _overFrame.Navigate(typeof(Page));
            _overFrame.BackStack.Clear();
            CheckSubPageStatus();

            if (pageType.FullName == lastSelectedPage)
            {
                return;
            }
        }

        if (
            lastSelectedPage == pageType.FullName
            && _navFrame.Content is not null
            && _navFrame.Content.GetType().FullName == lastSelectedPage
        )
        {
            return;
        }

        _settings.LastSelectedFeaturePage = pageType.FullName;
        _navFrame.Navigate(pageType, parameter, new EntranceNavigationTransitionInfo());
    }

    /// <inheritdoc/>
    public void NavigateToOver(Type pageType, object? parameter = null)
    {
        if (_overFrame is null)
        {
            throw new InvalidOperationException("导航框架未初始化.");
        }

        if (_overFrame.BackStack.Count > 0)
        {
            _overFrame.BackStack.Clear();
        }

        ActiveMainWindow();
        if (pageType == typeof(VideoPlayerPage) && _overFrame.Content is VideoPlayerPage page)
        {
            page.ViewModel.InitializePageCommand.Execute(parameter);
            IsOverlayOpen = true;
            return;
        }
        else if (pageType == typeof(PgcPlayerPage) && _overFrame.Content is PgcPlayerPage pgcPage)
        {
            pgcPage.ViewModel.InitializePageCommand.Execute(parameter);
            IsOverlayOpen = true;
            return;
        }

        _overFrame.Navigate(pageType, parameter, new EntranceNavigationTransitionInfo());
        IsOverlayOpen = true;
        CheckSubPageStatus();
    }

    /// <summary>
    /// 执行搜索.
    /// </summary>
    public void Search(string keyword)
    {
        if (_overFrame is not null && _overFrame.Content is SearchPage page)
        {
            page.ViewModel.SearchCommand.Execute(keyword);
            return;
        }

        NavigateToOver(typeof(SearchPage), keyword);
    }

    /// <summary>
    /// 设置导航条目可见性.
    /// </summary>
    public void SetNavItemVisibility(Type pageType, bool isVisible)
    {
        AppNavigationItemViewModel? item = MenuItems.FirstOrDefault(p =>
            p.PageKey == pageType.FullName
        );
        item?.IsVisible = isVisible;
    }

    /// <summary>
    /// 在指定区域执行搜索.
    /// </summary>
    public void SearchInRegion(string keyword)
    {
        if (_overFrame.Content is UserSpacePage usp)
        {
            usp.ViewModel.SearchCommand.Execute(keyword);
        }
        else if (_overFrame.Content is HistoryPage hp)
        {
            hp.ViewModel.SearchCommand.Execute(keyword);
        }
    }

    /// <summary>
    /// 尝试返回.
    /// </summary>
    public void Back()
    {
        for (int i = _overFrame.BackStack.Count - 1; i >= 0; i--)
        {
            if (_overFrame.BackStack[i].SourcePageType.FullName == typeof(Page).FullName)
            {
                _overFrame.BackStack.RemoveAt(i);
            }
        }

        if (_overFrame.CanGoBack)
        {
            _overFrame.GoBack();
        }
        else
        {
            _overFrame.Navigate(typeof(Page));
            _overFrame.BackStack.Clear();
            _overFrame.Content = null;
            IsOverlayOpen = false;
            _navFrame.Focus(FocusState.Programmatic);
        }

        CheckSubPageStatus();
    }

    /// <summary>
    /// 初始化导航视图模型.
    /// </summary>
    public void Initialize(Frame navFrame, Frame overFrame)
    {
        if (_navFrame is not null && _overFrame is not null)
        {
            return;
        }

        _navFrame = navFrame;
        _overFrame = overFrame;
        _overFrame.Navigated += OnOverlayNavigated;
        IsTopNavBarShown = _settings.IsTopNavBarShown;

        MenuItems = [.. GetMenuItems()];

        foreach (AppNavigationItemViewModel item in GetFooterItems())
        {
            FooterItems.Add(item);
        }
    }

    private IReadOnlyList<AppNavigationItemViewModel> GetMenuItems()
    {
        string? lastSelectedPage = string.IsNullOrEmpty(_settings.LastSelectedFeaturePage)
            ? typeof(PopularPage).FullName
            : _settings.LastSelectedFeaturePage;
        List<AppNavigationItemViewModel> list =
        [
            GetItem<PopularPage>(Resources.PopularSlim, FluentIcons.Common.Symbol.Rocket),
            GetItem<MomentPage>(Resources.DynamicFeed, FluentIcons.Common.Symbol.DesignIdeas),
            GetItem<VideoPartitionPage>(Resources.Video, FluentIcons.Common.Symbol.VideoClip),
            GetItem<AnimePage>(Resources.Anime, FluentIcons.Common.Symbol.Calendar),
            GetItem<CinemaPage>(Resources.Cinema, FluentIcons.Common.Symbol.SlideSearch),
            GetItem<ArticlePartitionPage>(
                Resources.Article,
                FluentIcons.Common.Symbol.DocumentBulletList
            ),
        ];

        foreach (AppNavigationItemViewModel item in list)
        {
            item.IsSelected = item.PageKey == lastSelectedPage && item.IsVisible;
        }

        if (!list.Any(p => p.IsSelected) && list.Any(p => p.IsVisible))
        {
            list.First(p => p.IsVisible).IsSelected = true;
        }

        return list;
    }

    private IReadOnlyList<AppNavigationItemViewModel> GetFooterItems()
    {
        List<AppNavigationItemViewModel> list =
        [
            // GetItem<DownloadPage>(Resources.Download, FluentIcons.Common.Symbol.CloudArrowDown),
            GetItem<MessagePage>(Resources.Message, FluentIcons.Common.Symbol.Chat),
            GetItem<SettingsPage>(Resources.Settings, FluentIcons.Common.Symbol.Settings),
        ];
        return list;
    }

    private AppNavigationItemViewModel GetItem<TPage>(
        string title,
        FluentIcons.Common.Symbol symbol,
        bool isSelected = false
    )
        where TPage : Page
    {
        bool isVisible = _settings.GetPageVisible(typeof(TPage));
        return new AppNavigationItemViewModel(
            this,
            typeof(TPage),
            title,
            symbol,
            isSelected,
            isVisible: isVisible
        );
    }

    private void ActiveMainWindow() => _appViewModel.Windows.Find(p => p is MainWindow)?.Activate();

    private void OnOverlayNavigated(object sender, NavigationEventArgs e)
    {
        OverlayNavigated?.Invoke(this, e.SourcePageType);
    }

    private void CheckSubPageStatus()
    {
        Type? overPageType = _overFrame.Content?.GetType();
        IsFavoritesPage = IsOverlayOpen && overPageType == typeof(FavoritesPage);
        IsHistoryPage = IsOverlayOpen && overPageType == typeof(HistoryPage);
        IsViewLaterPage = IsOverlayOpen && overPageType == typeof(ViewLaterPage);
    }
}
