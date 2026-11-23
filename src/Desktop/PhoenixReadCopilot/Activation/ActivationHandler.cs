namespace PhoenixReadCopilot.Activation;

/// <summary>
/// 泛型激活处理器基类.
/// </summary>
/// <typeparam name="T">激活参数类型.</typeparam>
internal abstract class ActivationHandler<T> : IActivationHandler
    where T : class
{
    /// <summary>
    /// 重写此方法以判断是否处理该类型激活.
    /// </summary>
    protected virtual bool CanHandleInternal(T args) => true;

    /// <summary>
    /// 重写此方法以实现具体的激活处理逻辑.
    /// </summary>
    protected abstract Task HandleInternalAsync(T args);

    public bool CanHandle(object args) => args is T t && CanHandleInternal(t);

    public async Task HandleAsync(object args)
    {
        if (args is T t)
        {
            await HandleInternalAsync(t);
        }
    }
}
