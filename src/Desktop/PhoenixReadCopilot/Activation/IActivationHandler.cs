namespace PhoenixReadCopilot.Activation;

/// <summary>
/// 应用程序生命周期与激活处理器接口.
/// </summary>
public interface IActivationHandler
{
    /// <summary>
    /// 判断当前处理器是否能处理给定的激活参数.
    /// </summary>
    bool CanHandle(object args);

    /// <summary>
    /// 执行激活处理逻辑.
    /// </summary>
    Task HandleAsync(object args);
}
