using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 根布局视图模型.
/// </summary>
internal sealed partial class RootLayoutViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly INavigationViewService _navigationViewService;
    private readonly IAppLifecycleService _lifecycleService;

    [ObservableProperty]
    public partial bool IsInitialLoading { get; set; }

    [ObservableProperty]
    public partial bool IsOverlayOpen { get; set; }

    [ObservableProperty]
    public partial bool IsTopNavBarShown { get; set; }

    [ObservableProperty]
    public partial bool IsFavoritePage { get; set; }

    [ObservableProperty]
    public partial bool IsHistoryPage { get; set; }

    [ObservableProperty]
    public partial bool IsViewLaterPage { get; set; }

    /// <summary>
    /// 主菜单条目列表.
    /// </summary>
    public ObservableCollection<AppNavigationItemViewModel> MenuItems =>
        _navigationViewService.MenuItems;

    /// <summary>
    /// 底部菜单条目列表.
    /// </summary>
    public ObservableCollection<AppNavigationItemViewModel> FooterItems =>
        _navigationViewService.FooterItems;

    public RootLayoutViewModel(
        INavigationService navigationService,
        INavigationViewService navigationViewService,
        IAppLifecycleService lifecycleService
    )
    {
        _navigationService = navigationService;
        _navigationViewService = navigationViewService;
        _lifecycleService = lifecycleService;

        IsInitialLoading = _lifecycleService.IsInitialLoading;
        _lifecycleService.InitialLoadingChanged += OnInitialLoadingChanged;

        IsOverlayOpen = _navigationService.IsOverlayOpen;
        IsTopNavBarShown = _navigationService.IsTopNavBarShown;
        IsFavoritePage = _navigationService.IsFavoritePage;
        IsHistoryPage = _navigationService.IsHistoryPage;
        IsViewLaterPage = _navigationService.IsViewLaterPage;

        _navigationService.OverlayStateChanged += OnOverlayStateChanged;
        _navigationService.SubPageStatusChanged += OnSubPageStatusChanged;
    }

    private void OnOverlayStateChanged(object? sender, bool isOpen)
    {
        IsOverlayOpen = isOpen;
    }

    private void OnInitialLoadingChanged(object? sender, bool isLoading)
    {
        IsInitialLoading = isLoading;
    }

    private void OnSubPageStatusChanged(object? sender, EventArgs e)
    {
        IsOverlayOpen = _navigationService.IsOverlayOpen;
        IsTopNavBarShown = _navigationService.IsTopNavBarShown;
        IsFavoritePage = _navigationService.IsFavoritePage;
        IsHistoryPage = _navigationService.IsHistoryPage;
        IsViewLaterPage = _navigationService.IsViewLaterPage;
    }

    /// <summary>
    /// 返回上一页或关闭浮层.
    /// </summary>
    public void Back() => _navigationService.GoBack();

    /// <summary>
    /// 导航到浮层页面.
    /// </summary>
    public void NavigateToOver(Type pageType, object? parameter = null) =>
        _navigationService.NavigateToOver(pageType, parameter);

    /// <summary>
    /// 导航到主页面.
    /// </summary>
    public void NavigateTo(Type pageType, object? parameter = null) =>
        _navigationService.NavigateTo(pageType, parameter);
}
