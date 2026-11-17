namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 导航服务接口.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 主页面导航完成事件.
    /// </summary>
    event EventHandler<Type>? Navigated;

    /// <summary>
    /// 浮层页面导航完成事件.
    /// </summary>
    event EventHandler<Type>? OverlayNavigated;

    /// <summary>
    /// 浮层开闭状态变化事件.
    /// </summary>
    event EventHandler<bool>? OverlayStateChanged;

    /// <summary>
    /// 子页面/浮层页面状态变化事件.
    /// </summary>
    event EventHandler? SubPageStatusChanged;

    /// <summary>
    /// 是否可以后退.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// 浮层是否处于打开状态.
    /// </summary>
    bool IsOverlayOpen { get; set; }

    /// <summary>
    /// 是否显示顶部导航栏.
    /// </summary>
    bool IsTopNavBarShown { get; set; }

    /// <summary>
    /// 当前浮层是否为收藏页.
    /// </summary>
    bool IsFavoritePage { get; }

    /// <summary>
    /// 当前浮层是否为历史记录页.
    /// </summary>
    bool IsHistoryPage { get; }

    /// <summary>
    /// 当前浮层是否为稍后再看页.
    /// </summary>
    bool IsViewLaterPage { get; }

    /// <summary>
    /// 导航到指定页面（通过页面键名或 ViewModel 键名）.
    /// </summary>
    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);

    /// <summary>
    /// 导航到指定页面类型.
    /// </summary>
    bool NavigateTo(Type pageType, object? parameter = null, bool clearNavigation = false);

    /// <summary>
    /// 导航到浮层页面（通过页面键名或 ViewModel 键名）.
    /// </summary>
    bool NavigateToOver(string pageKey, object? parameter = null);

    /// <summary>
    /// 导航到浮层页面类型.
    /// </summary>
    bool NavigateToOver(Type pageType, object? parameter = null);

    /// <summary>
    /// 返回上一页或关闭浮层.
    /// </summary>
    bool GoBack();

    /// <summary>
    /// 全局搜索.
    /// </summary>
    void Search(string keyword);

    /// <summary>
    /// 在当前区域内搜索.
    /// </summary>
    void SearchInRegion(string keyword);
}
