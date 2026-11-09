using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.Controls;

/// <summary>
/// 根布局，用于包裹整个应用程序的布局.
/// </summary>
public sealed partial class RootLayout : RootLayoutBase
{
    private readonly AppViewModel _appViewModel = Ioc.Default.GetRequiredService<AppViewModel>();

    /// <summary>
    /// Initializes a new instance of the <see cref="RootLayout"/> class.
    /// </summary>
    public RootLayout()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 获取主标题栏.
    /// </summary>
    /// <returns><see cref="AppTitleBar"/>.</returns>
    public AppTitleBar GetMainTitleBar() => MainTitleBar;

    /// <summary>
    /// 获取二级标题栏.
    /// </summary>
    /// <returns><see cref="AppTitleBar"/>.</returns>
    public AppTitleBar GetSecondaryTitleBar() => SecondaryTitleBar;

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
        SecondaryTitleBar.IsBackEnabled = isSecondaryTitleBarVisible;
        _appViewModel.ActivatedWindow.AppWindow.TitleBar.PreferredHeightOption =
            TitleBarHeightOption.Standard;
        NavView.IsPaneOpen = false;
        VisualStateManager.GoToState(NavView, "PaneCollapsed", false);
        VisualStateManager.GoToState(this, nameof(PlayerState), false);

        if (OverlayFrame.Content is VideoPlayerPage vPage)
        {
            SecondaryTitleBar.Title = vPage.ViewModel.Title;
            vPage.EnterPlayerHostMode();
        }
        else if (OverlayFrame.Content is PgcPlayerPage pPage)
        {
            SecondaryTitleBar.Title = pPage.ViewModel.EpisodeTitle;
            pPage.EnterPlayerHostMode();
        }

        if (isSecondaryTitleBarVisible)
        {
            SecondaryTitleBar.UpdatePadding();
            SecondaryTitleBar.DelayUpdateAsync(100);
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
        _appViewModel.ActivatedWindow.AppWindow.TitleBar.PreferredHeightOption =
            TitleBarHeightOption.Tall;
        NavView.IsPaneOpen = true;
        VisualStateManager.GoToState(NavView, "PaneVisible", false);
        VisualStateManager.GoToState(this, nameof(NormalState), false);

        if (OverlayFrame.Content is VideoPlayerPage vPage)
        {
            vPage.ExitPlayerHostMode();
        }
        else if (OverlayFrame.Content is PgcPlayerPage pPage)
        {
            pPage.ExitPlayerHostMode();
        }

        MainTitleBar.UpdatePadding();
        MainTitleBar.DelayUpdateAsync(100);
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
            vPage.ViewModel.Player.BackToDefaultModeCommand.Execute(null);
            isHandled = true;
        }
        else if (OverlayFrame.Content is PgcPlayerPage pPage)
        {
            pPage.ViewModel.Player.BackToDefaultModeCommand.Execute(null);
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
        else if (OverlayFrame.Content is PgcPlayerPage pPage)
        {
            return pPage.ViewModel.Player.TryTogglePlayPause();
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
        else if (OverlayFrame.Content is PgcPlayerPage pPage)
        {
            return pPage.MarkRightArrowPressed();
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
        else if (OverlayFrame.Content is PgcPlayerPage pPage)
        {
            return pPage.CancelRightArrow();
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        ViewModel.Initialize(MainFrame, OverlayFrame);
        AppNavigationItemViewModel? selectedItem = ViewModel.MenuItems.FirstOrDefault(p =>
            p.IsSelected
        );
        if (selectedItem is not null)
        {
            NavView.SelectedItem = selectedItem;
            selectedItem.NavigateCommand.Execute(null);
        }
    }

    private void OnNavViewBackRequested(
        NavigationView sender,
        NavigationViewBackRequestedEventArgs args
    ) => OnBackRequested(null, null);

    private void OnNavViewItemInvoked(
        NavigationView sender,
        NavigationViewItemInvokedEventArgs args
    )
    {
        _ = this;
        AppNavigationViewItem? item = args.InvokedItemContainer as AppNavigationViewItem;
        AppNavigationItemViewModel? context = item?.Tag as AppNavigationItemViewModel;
        context?.NavigateCommand.Execute(null);
    }

    private void OnBackRequested(object sender, EventArgs e) => ViewModel.Back();

    private void OnUpdateActionButtonClick(TeachingTip sender, object args) =>
        _appViewModel.ShowUpdateCommand.Execute(null);

    private void OnUpdateCloseButtonClick(TeachingTip sender, object args) =>
        _appViewModel.HideUpdateCommand.Execute(null);

    private void OnFavoriteButtonClick(object sender, EventArgs e) =>
        ViewModel.NavigateToOver(typeof(FavoritesPage), null);

    private void OnViewLaterButtonClick(object sender, EventArgs e) =>
        ViewModel.NavigateToOver(typeof(ViewLaterPage), null);

    private void OnHistoryButtonClick(object sender, EventArgs e) =>
        ViewModel.NavigateToOver(typeof(HistoryPage), null);

    private void OnVideoBackRequested(object sender, EventArgs e) =>
        TryBackToDefaultIfPlayerHostMode();
}

/// <summary>
/// 根布局基类.
/// </summary>
public abstract class RootLayoutBase : LayoutUserControlBase<NavigationViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RootLayoutBase"/> class.
    /// </summary>
    protected RootLayoutBase() => ViewModel = Ioc.Default.GetRequiredService<NavigationViewModel>();
}
