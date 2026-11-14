namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 通知严重性/类型枚举.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 普通提示.
    /// </summary>
    Information,

    /// <summary>
    /// 成功提示.
    /// </summary>
    Success,

    /// <summary>
    /// 警告提示.
    /// </summary>
    Warning,

    /// <summary>
    /// 错误提示.
    /// </summary>
    Error,
}

/// <summary>
/// 全局通知服务契约.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 显示应用内轻量提示（状态栏/Toast浮层）.
    /// </summary>
    /// <param name="message">提示内容.</param>
    /// <param name="type">通知类型.</param>
    Task ShowInAppNotificationAsync(
        string message,
        NotificationType type = NotificationType.Information
    );

    /// <summary>
    /// 发送系统原生通知 (Windows Toast).
    /// </summary>
    /// <param name="title">通知标题.</param>
    /// <param name="message">通知正文.</param>
    void ShowSystemNotification(string title, string message);

    /// <summary>
    /// 发送或更新带进度条的通知（支持多集下载等批处理任务进度显示）.
    /// </summary>
    /// <param name="tag">通知唯一标识/Tag.</param>
    /// <param name="title">通知标题.</param>
    /// <param name="message">通知正文/状态描述.</param>
    /// <param name="progress">进度 (0.0 ~ 1.0).</param>
    /// <param name="isCompleted">是否已完成任务.</param>
    void UpdateProgressNotification(
        string tag,
        string title,
        string message,
        double progress,
        bool isCompleted = false
    );
}
