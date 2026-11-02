namespace Danmaku.Core.Models;

/// <summary>
/// 弹幕默认图层定义.
/// </summary>
public static class DanmakuDefaultLayerDef
{
    /// <summary>
    /// 默认图层 ID.
    /// </summary>
    public const uint DefaultLayerId = 0;

    /// <summary>
    /// 滚动弹幕图层 ID.
    /// </summary>
    public const uint RollingLayerId = 0;

    /// <summary>
    /// 逆向滚动弹幕图层 ID.
    /// </summary>
    public const uint ReverseRollingLayerId = 1;

    /// <summary>
    /// 顶部弹幕图层 ID.
    /// </summary>
    public const uint TopLayerId = 2;

    /// <summary>
    /// 底部弹幕图层 ID.
    /// </summary>
    public const uint BottomLayerId = 3;

    /// <summary>
    /// 高级弹幕图层 ID.
    /// </summary>
    public const uint AdvancedLayerId = 4;

    /// <summary>
    /// 字幕弹幕图层 ID.
    /// </summary>
    public const uint SubtitleLayerId = 5;

    /// <summary>
    /// 默认图层总数.
    /// </summary>
    public const uint DefaultLayerCount = 6;
}
