namespace WinUIKernel.Share;

/// <summary>
/// AI kernel extension.
/// </summary>
public static class WinUIKernelShareExtensions
{
    /// <summary>
    /// 是否启用卡片动画。
    /// </summary>
    public static bool IsCardAnimationEnabled { get; set; } = true;

    /// <summary>
    /// 是否启用卡片阴影。
    /// </summary>
    public static bool IsCardShadowEnabled { get; set; }

    /// <summary>
    /// 图片缓存时间。
    /// </summary>
    public static TimeSpan ImageCacheTime { get; set; } = TimeSpan.FromDays(7);
}
