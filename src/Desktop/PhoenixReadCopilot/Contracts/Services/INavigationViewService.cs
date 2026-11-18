using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.Contracts.Services;

/// <summary>
/// 导航视图条目管理服务接口.
/// </summary>
public interface INavigationViewService
{
    /// <summary>
    /// 主菜单条目列表.
    /// </summary>
    ObservableCollection<AppNavigationItemViewModel> MenuItems { get; }

    /// <summary>
    /// 底部菜单条目列表.
    /// </summary>
    ObservableCollection<AppNavigationItemViewModel> FooterItems { get; }

    /// <summary>
    /// 初始化并绑定 NavigationView 控件.
    /// </summary>
    /// <param name="navigationView">NavigationView 控件.</param>
    void Initialize(NavigationView navigationView);

    /// <summary>
    /// 注销控件事件.
    /// </summary>
    void UnregisterEvents();

    /// <summary>
    /// 根据页面类型获取导航项.
    /// </summary>
    /// <param name="pageType">页面类型.</param>
    /// <returns>匹配的导航项.</returns>
    AppNavigationItemViewModel? GetSelectedItem(Type pageType);

    /// <summary>
    /// 设置导航项可见性.
    /// </summary>
    /// <param name="pageType">页面类型.</param>
    /// <param name="isVisible">是否可见.</param>
    void SetNavItemVisibility(Type pageType, bool isVisible);

    /// <summary>
    /// 设置导航项可见性.
    /// </summary>
    /// <param name="pageKey">页面标识.</param>
    /// <param name="isVisible">是否可见.</param>
    void SetNavItemVisibility(string pageKey, bool isVisible);
}
