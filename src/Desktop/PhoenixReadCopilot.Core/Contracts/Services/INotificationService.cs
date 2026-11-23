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
    Task ShowInAppNotificationAsync(string message, NotificationType type = NotificationType.Information);

    /// <summary>
    /// 显示应用内轻量提示 (同步触发).
    /// </summary>
    /// <param name="message">提示内容.</param>
    /// <param name="type">通知类型.</param>
    void ShowInAppNotification(string message, NotificationType type = NotificationType.Information);

    /// <summary>
    /// 发送系统原生通知 (Windows Toast).
    /// </summary>
    /// <param name="title">通知标题.</param>
    /// <param name="message">通知正文.</param>
    /// <param name="logoUri">自定义图标/封面 URL 或本地文件绝对路径（可选）.</param>
    /// <param name="folderPath">可选关联文件夹路径（若提供则在通知底部附带“打开文件夹”操作按钮）.</param>
    void ShowSystemNotification(
        string title,
        string message,
        string? logoUri = null,
        string? folderPath = null
    );

    /// <summary>
    /// 发送或更新带进度条的系统原生通知.
    /// </summary>
    /// <param name="tag">通知唯一标识/Tag.</param>
    /// <param name="headerTitle">通知顶部大标题（例如：“正在下载 剧名”）.</param>
    /// <param name="progressBarTitle">进度条上的标题（例如：“剧名”）.</param>
    /// <param name="status">进度条下方的状态描述（例如：“已下载 3/10 集” 或 “全部 10 集已下载完成！”）.</param>
    /// <param name="progress">进度 (0.0 ~ 1.0).</param>
    /// <param name="isCompleted">是否已完成任务.</param>
    /// <param name="logoUri">自定义图标/封面 URL 或本地路径（可选）.</param>
    void UpdateProgressNotification(
        string tag,
        string headerTitle,
        string progressBarTitle,
        string status,
        double progress,
        bool isCompleted = false,
        string? logoUri = null
    );

    /// <summary>
    /// 移除指定 Tag 的通知（例如进度通知完成后移除以替换为带按钮的完成卡片）.
    /// </summary>
    /// <param name="tag">通知 Tag.</param>
    void RemoveNotification(string tag);

    /// <summary>
    /// 异步移除指定 Tag 的通知.
    /// </summary>
    /// <param name="tag">通知 Tag.</param>
    Task RemoveNotificationAsync(string tag);
}
