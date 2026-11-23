namespace PhoenixReadCopilot.Contracts.Services;

/// <summary>
/// 应用程序生命周期与全局状态服务契约.
/// </summary>
internal interface IAppLifecycleService
{
    /// <summary>
    /// 应用是否处于初始加载中.
    /// </summary>
    bool IsInitialLoading { get; set; }

    /// <summary>
    /// 初始加载状态变化事件.
    /// </summary>
    event EventHandler<bool>? InitialLoadingChanged;

    /// <summary>
    /// 执行生命周期启动准备（清除应用徽标角标，广播全局初始加载中状态）.
    /// </summary>
    void Launch();

    /// <summary>
    /// 清除应用徽标角标通知 (Badge).
    /// </summary>
    void ClearBadge();

    /// <summary>
    /// 重启应用程序.
    /// </summary>
    void Restart();
}
