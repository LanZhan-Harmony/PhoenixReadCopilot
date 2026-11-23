namespace PhoenixReadCopilot.Contracts.Services;

/// <summary>
/// 页面定位服务接口.
/// </summary>
internal interface IPageService
{
    /// <summary>
    /// 根据页面或 ViewModel 键名获取页面类型.
    /// </summary>
    /// <param name="key">页面或 ViewModel 键名.</param>
    /// <returns>页面类型.</returns>
    Type GetPageType(string key);
}
