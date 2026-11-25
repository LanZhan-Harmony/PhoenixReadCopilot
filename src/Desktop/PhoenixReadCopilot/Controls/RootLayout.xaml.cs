using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls;

/// <summary>
/// 根布局，用于包裹整个应用程序的布局.
/// </summary>
internal sealed partial class RootLayout : UserControl
{
    private readonly IWindowService _windowService = Ioc.Default.GetRequiredService<IWindowService>();
    private readonly INavigationService _navigationService =
        Ioc.Default.GetRequiredService<INavigationService>();
    private readonly INavigationViewService _navigationViewService =
        Ioc.Default.GetRequiredService<INavigationViewService>();

    public RootLayoutViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<RootLayoutViewModel>();

    public RootLayout()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 获取主标题栏.
    /// </summary>
    /// <returns><see cref="TitleBar"/>.</returns>
    public TitleBar GetMainTitleBar() => MainTitleBar;

    /// <summary>
    /// 获取二级标题栏.
    /// </summary>
    /// <returns><see cref="TitleBar"/>.</returns>
    public TitleBar GetSecondaryTitleBar() => SecondaryTitleBar;

    /// <summary>
    /// 准备隐藏除播放器外的其它控件.
    /// </summary>
    public void PrepareFullPlayerPresenter(PlayerDisplayMode mode)
    {
        if (!ViewModel.IsOverlayOpen)
        {
            return;
        }

        MainTitleBar.Visibility = Visibility.Collapsed;
        bool isSecondaryTitleBarVisible =
            mode == PlayerDisplayMode.FullWindow || mode == PlayerDisplayMode.CompactOverlay;
        SecondaryTitleBar.Visibility = isSecondaryTitleBarVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
        SecondaryTitleBar.IsBackButtonVisible = isSecondaryTitleBarVisible;
        SecondaryTitleBar.IsBackButtonEnabled = isSecondaryTitleBarVisible;
        if (_windowService.ActiveWindow?.AppWindow?.TitleBar is { } titleBar)
        {
            titleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
        }
        NavView.IsPaneOpen = false;
        VisualStateManager.GoToState(NavView, "PaneCollapsed", false);
        VisualStateManager.GoToState(this, nameof(PlayerState), false);

        if (OverlayFrame.Content is VideoPlayerPage vPage)
        {
            SecondaryTitleBar.Title = vPage.ViewModel.Title;
            vPage.EnterPlayerHostMode();
        }
    }

    /// <summary>
    /// 播放器解除占用状态，显示其它控件.
    /// </summary>
    public void ExitFullPlayerPresenter()
    {
        if (!ViewModel.IsOverlayOpen)
        {
            return;
        }

        MainTitleBar.Visibility = Visibility.Visible;
        SecondaryTitleBar.Visibility = Visibility.Collapsed;
        SecondaryTitleBar.IsBackButtonVisible = false;
        if (_windowService.ActiveWindow?.AppWindow?.TitleBar is { } titleBar)
        {
            titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        }
        NavView.IsPaneOpen = true;
        VisualStateManager.GoToState(NavView, "PaneVisible", false);
        VisualStateManager.GoToState(this, nameof(NormalState), false);

        if (OverlayFrame.Content is VideoPlayerPage vPage)
        {
            vPage.ExitPlayerHostMode();
        }
    }

    /// <summary>
    /// 是否处于独占播放模式.
    /// </summary>
    /// <returns>结果.</returns>
    public bool TryBackToDefaultIfPlayerHostMode()
    {
        if (MainTitleBar.Visibility == Visibility.Visible)
        {
            return false;
        }

        bool isHandled = false;
        if (OverlayFrame.Content is VideoPlayerPage vPage)
        {
            vPage.ViewModel.Player.BackToDefaultMode();
            isHandled = true;
        }

        return isHandled;
    }

    /// <summary>
    /// 尝试在播放器中切换播放暂停状态.
    /// </summary>
    /// <returns>是否处理.</returns>
    public bool TryTogglePlayPauseIfInPlayer()
    {
        if (!ViewModel.IsOverlayOpen)
        {
            return false;
        }

        if (OverlayFrame.Content is VideoPlayerPage vPage)
        {
            return vPage.ViewModel.Player.TryTogglePlayPause();
        }

        return false;
    }

    /// <summary>
    /// 尝试标记右箭头按下时间.
    /// </summary>
    /// <returns>是否处理.</returns>
    public bool TryMarkRightArrowPressedTime()
    {
        if (!ViewModel.IsOverlayOpen)
        {
            return false;
        }

        if (OverlayFrame.Content is VideoPlayerPage vPage)
        {
            return vPage.MarkRightArrowPressed();
        }

        return false;
    }

    /// <summary>
    /// 尝试取消右箭头按下.
    /// </summary>
    /// <returns>是否处理.</returns>
    public bool TryCancelRightArrow()
    {
        if (!ViewModel.IsOverlayOpen)
        {
            return false;
        }

        if (OverlayFrame.Content is VideoPlayerPage vPage)
        {
            return vPage.CancelRightArrow();
        }

        return false;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_navigationService is NavigationService navService)
        {
            navService.Initialize(MainFrame, OverlayFrame);
        }
        _navigationViewService.Initialize(NavView);

        AppNavigationItemViewModel? selectedItem = _navigationViewService.MenuItems.FirstOrDefault(
            p => p.IsSelected
        );
        if (selectedItem is not null)
        {
            NavView.SelectedItem = selectedItem;
            selectedItem.NavigateCommand.Execute(null);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _navigationViewService.UnregisterEvents();
    }

    private void OnBackRequested(TitleBar sender, object args) => ViewModel.Back();

    private void OnFavoriteButtonClick(object sender, EventArgs e) =>
        ViewModel.NavigateTo(typeof(UserSpacePage), "Favorite");

    private void OnViewLaterButtonClick(object sender, EventArgs e) =>
        ViewModel.NavigateTo(typeof(UserSpacePage), "ViewLater");

    private void OnHistoryButtonClick(object sender, EventArgs e) =>
        ViewModel.NavigateTo(typeof(UserSpacePage), "History");

    private void OnVideoBackRequested(TitleBar sender, object args) =>
        TryBackToDefaultIfPlayerHostMode();
}
