namespace PhoenixReadCopilot.Models.User;

/// <summary>
/// 用户头像模型.
/// </summary>
public sealed class UserAvatar
{
    /// <summary>
    /// 头像地址.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <summary>
    /// 头像源地址 (兼容 XAML 绑定).
    /// </summary>
    public Uri? SourceUri => Uri;

    /// <summary>
    /// 兼容应用图片模型.
    /// </summary>
    public AppImage? AppImage => Uri is not null ? new AppImage(Uri) : null;
}
