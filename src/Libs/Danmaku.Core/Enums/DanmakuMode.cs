namespace Danmaku.Core.Enums;

/// <summary>
/// 弹幕模式枚举.
/// </summary>
public enum DanmakuMode
{
    /// <summary>
    /// 未知模式.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 滚动弹幕.
    /// </summary>
    Rolling = 1,

    /// <summary>
    /// 底部弹幕.
    /// </summary>
    Bottom = 4,

    /// <summary>
    /// 顶部弹幕.
    /// </summary>
    Top = 5,

    /// <summary>
    /// 逆向滚动弹幕.
    /// </summary>
    ReverseRolling = 6,

    /// <summary>
    /// 字幕弹幕.
    /// </summary>
    Subtitle = 7,
}
