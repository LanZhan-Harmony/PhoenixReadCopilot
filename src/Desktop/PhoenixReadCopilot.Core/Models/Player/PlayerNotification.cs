namespace PhoenixReadCopilot.Core.Models.Player;

/// <summary>
/// 播放器通知.
/// </summary>
public sealed class PlayerNotification(Action action, string message, int duration)
{
    /// <summary>
    /// 点击通知执行的操作.
    /// </summary>
    public Action Action { get; } = action;

    /// <summary>
    /// 通知消息.
    /// </summary>
    public string? Message { get; } = message;

    /// <summary>
    /// 通知持续时间.
    /// </summary>
    public int Duration { get; } = duration;
}
