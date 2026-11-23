using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 页面导航服务实现.
/// </summary>
internal sealed class NavigationService : INavigationService
{
    private readonly IPageService _pageService;
    private readonly IWindowService _windowService;
    private readonly AppSettings _settings;

    private Frame? _frame;
    private Frame? _overFrame;
    private bool _isOverlayOpen;
    private bool _isTopNavBarShown;

    /// <inheritdoc/>
    public event EventHandler<Type>? Navigated;

    /// <inheritdoc/>
    public event EventHandler<Type>? OverlayNavigated;

    /// <inheritdoc/>
    public event EventHandler<bool>? OverlayStateChanged;

    /// <inheritdoc/>
    public event EventHandler? SubPageStatusChanged;

    /// <summary>
    /// 主页面导航框架.
    /// </summary>
    public Frame? Frame
    {
        get => _frame;
        set
        {
            UnregisterFrameEvents();
            _frame = value;
            RegisterFrameEvents();
        }
    }

    /// <summary>
    /// 浮层导航框架.
    /// </summary>
    public Frame? OverlayFrame
    {
        get => _overFrame;
        set
        {
            UnregisterOverlayEvents();
            _overFrame = value;
            RegisterOverlayEvents();
        }
    }

    /// <inheritdoc/>
    public bool CanGoBack => IsOverlayOpen || (_frame != null && _frame.CanGoBack);

    /// <inheritdoc/>
    public bool IsOverlayOpen
    {
        get => _isOverlayOpen;
        set
        {
            if (_isOverlayOpen != value)
            {
                _isOverlayOpen = value;
                OverlayStateChanged?.Invoke(this, value);
                CheckSubPageStatus();
            }
        }
    }

    /// <inheritdoc/>
    public bool IsTopNavBarShown
    {
        get => _isTopNavBarShown;
        set
        {
            if (_isTopNavBarShown != value)
            {
                _isTopNavBarShown = value;
                _settings.IsTopNavBarShown = value;
                SubPageStatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <inheritdoc/>
    public bool IsFavoritePage { get; private set; }

    /// <inheritdoc/>
    public bool IsHistoryPage { get; private set; }

    /// <inheritdoc/>
    public bool IsViewLaterPage { get; private set; }

    public NavigationService(
        IPageService pageService,
        ISettingsService settingsService,
        IWindowService windowService
    )
    {
        _pageService = pageService;
        _settings = settingsService.Settings;
        _windowService = windowService;
        _isTopNavBarShown = _settings.IsTopNavBarShown;
    }

    /// <summary>
    /// 初始化导航框架.
    /// </summary>
    public void Initialize(Frame navFrame, Frame overFrame)
    {
        Frame = navFrame;
        OverlayFrame = overFrame;
    }

    /// <inheritdoc/>
    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        Type pageType = _pageService.GetPageType(pageKey);
        return NavigateTo(pageType, parameter, clearNavigation);
    }

    /// <inheritdoc/>
    public bool NavigateTo(Type pageType, object? parameter = null, bool clearNavigation = false)
    {
        if (_frame is null)
        {
            throw new InvalidOperationException("导航框架未初始化.");
        }

        ActiveMainWindow();
        string lastSelectedPage = _settings.LastSelectedFeaturePage;
        if (IsOverlayOpen)
        {
            IsOverlayOpen = false;
            _overFrame?.Navigate(typeof(Page));
            _overFrame?.BackStack.Clear();
            CheckSubPageStatus();

            if (pageType.FullName == lastSelectedPage)
            {
                return true;
            }
        }

        if (
            lastSelectedPage == pageType.FullName
            && _frame.Content is not null
            && _frame.Content.GetType().FullName == lastSelectedPage
        )
        {
            if (parameter is not null && _frame.Content is UserSpacePage userSpacePage)
            {
                _ = userSpacePage.ViewModel.InitializeAsync(parameter);
            }
            return true;
        }

        _settings.LastSelectedFeaturePage = pageType.FullName ?? string.Empty;
        bool navigated = _frame.Navigate(
            pageType,
            parameter,
            new EntranceNavigationTransitionInfo()
        );
        _frame.BackStack.Clear();
        return navigated;
    }

    /// <inheritdoc/>
    public bool NavigateToOver(string pageKey, object? parameter = null)
    {
        Type pageType = _pageService.GetPageType(pageKey);
        return NavigateToOver(pageType, parameter);
    }

    /// <inheritdoc/>
    public bool NavigateToOver(Type pageType, object? parameter = null)
    {
        if (_overFrame is null)
        {
            throw new InvalidOperationException("浮层导航框架未初始化.");
        }

        if (_overFrame.BackStack.Count > 0)
        {
            _overFrame.BackStack.Clear();
        }

        ActiveMainWindow();
        if (pageType == typeof(VideoPlayerPage) && _overFrame.Content is VideoPlayerPage page)
        {
            _ = page.ViewModel.InitializePageAsync(parameter);
            IsOverlayOpen = true;
            CheckSubPageStatus();
            return true;
        }

        bool navigated = _overFrame.Navigate(
            pageType,
            parameter,
            new EntranceNavigationTransitionInfo()
        );
        IsOverlayOpen = true;
        CheckSubPageStatus();
        return navigated;
    }

    /// <inheritdoc/>
    public bool GoBack()
    {
        if (_overFrame is null || _frame is null)
        {
            return false;
        }

        if (IsOverlayOpen)
        {
            for (int i = _overFrame.BackStack.Count - 1; i >= 0; i--)
            {
                if (_overFrame.BackStack[i].SourcePageType == typeof(Page))
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
                _frame.Focus(FocusState.Programmatic);
            }

            CheckSubPageStatus();
            return true;
        }

        if (_frame.CanGoBack)
        {
            _frame.GoBack();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Search(string keyword)
    {
        if (_overFrame is not null && _overFrame.Content is SearchPage page)
        {
            _ = page.ViewModel.SearchAsync(keyword);
            return;
        }

        NavigateToOver(typeof(SearchPage), keyword);
    }

    /// <inheritdoc/>
    public void SearchInRegion(string keyword)
    {
        if ((_overFrame?.Content as UserSpacePage ?? _frame?.Content as UserSpacePage) is { } usp)
        {
            usp.ViewModel.Search(keyword);
        }
    }

    private void RegisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.CacheSize = 0;
            _frame.Navigated += OnFrameNavigated;
        }
    }

    private void UnregisterFrameEvents()
    {
        _frame?.Navigated -= OnFrameNavigated;
    }

    private void RegisterOverlayEvents()
    {
        _overFrame?.Navigated += OnOverlayNavigated;
    }

    private void UnregisterOverlayEvents()
    {
        _overFrame?.Navigated -= OnOverlayNavigated;
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        CheckSubPageStatus();
        Navigated?.Invoke(this, e.SourcePageType);
    }

    private void OnOverlayNavigated(object sender, NavigationEventArgs e)
    {
        CheckSubPageStatus();
        OverlayNavigated?.Invoke(this, e.SourcePageType);
    }

    private void CheckSubPageStatus()
    {
        if (!IsOverlayOpen && _frame?.Content is UserSpacePage userSpacePage)
        {
            string? sectionId = userSpacePage.ViewModel.SelectedSection?.Id;
            IsFavoritePage = userSpacePage.ViewModel.IsMe && sectionId == "Favorite";
            IsHistoryPage = userSpacePage.ViewModel.IsMe && sectionId == "History";
            IsViewLaterPage = userSpacePage.ViewModel.IsMe && sectionId == "ViewLater";
        }
        else
        {
            IsFavoritePage = false;
            IsHistoryPage = false;
            IsViewLaterPage = false;
        }

        SubPageStatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ActiveMainWindow() => _windowService.GetWindow<MainWindow>()?.Activate();
}
