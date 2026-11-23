namespace PhoenixReadCopilot.Contracts.Services;

/// <summary>
/// 导航服务视图模型.
/// </summary>
internal interface INavServiceViewModel
{
    /// <summary>
    /// 导航到指定页面.
    /// </summary>
    void NavigateTo(Type pageType, object? parameter = null);
}
