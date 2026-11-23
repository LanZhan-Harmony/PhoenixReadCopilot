namespace PhoenixReadCopilot.Contracts.Services;

/// <summary>
/// 应用程序生命周期启动与激活路由服务契约.
/// </summary>
internal interface IActivationService
{
    /// <summary>
    /// 处理应用生命周期启动与参数激活.
    /// </summary>
    /// <param name="activationArgs">启动参数对象 (如 LaunchActivatedEventArgs 或 AppActivationArguments).</param>
    Task ActivateAsync(object activationArgs);
}
