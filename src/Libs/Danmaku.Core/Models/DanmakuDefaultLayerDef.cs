namespace Danmaku.Core.Models;

/// <summary>
/// 弹幕默认图层定义.
/// </summary>
internal static class DanmakuDefaultLayerDef
{
    /// <summary>
    /// 默认图层 ID.
    /// </summary>
    internal const uint DefaultLayerId = 0;

    /// <summary>
    /// 滚动弹幕图层 ID.
    /// </summary>
    internal const uint RollingLayerId = 0;

    /// <summary>
    /// 逆向滚动弹幕图层 ID.
    /// </summary>
    internal const uint ReverseRollingLayerId = 1;

    /// <summary>
    /// 顶部弹幕图层 ID.
    /// </summary>
    internal const uint TopLayerId = 2;

    /// <summary>
    /// 底部弹幕图层 ID.
    /// </summary>
    internal const uint BottomLayerId = 3;

    /// <summary>
    /// 字幕弹幕图层 ID.
    /// </summary>
    internal const uint SubtitleLayerId = 4;

    /// <summary>
    /// 默认图层总数.
    /// </summary>
    internal const uint DefaultLayerCount = 5;
}
