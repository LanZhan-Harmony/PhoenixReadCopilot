using Windows.UI;

namespace Danmaku.Core;

/// <summary>
/// 弹幕配置类，统一管理弹幕的各种显示和行为参数.
/// 所有属性均为可空类型，支持部分更新（只更新有值的属性）.
/// </summary>
public sealed class DanmakuConfiguration
{
    /// <summary>
    /// 默认字体名称.
    /// </summary>
    public const string DefaultFontFamilyName = "Microsoft YaHei";

    #region 公开属性（可空类型，支持部分更新）

    /// <summary>
    /// 是否自动控制密度.
    /// </summary>
    public bool? AutoControlDensity { get; set; }

    /// <summary>
    /// 文本是否粗体.
    /// </summary>
    public bool? IsTextBold { get; set; }

    /// <summary>
    /// 字幕是否不重叠.
    /// </summary>
    public bool? NoOverlapSubtitle { get; set; }

    /// <summary>
    /// 是否启用并行更新（多线程优化）.
    /// </summary>
    public bool? EnableParallelUpdate { get; set; }

    /// <summary>
    /// 滚动弹幕密度限制.
    /// -1 表示无限制，1-10 分别对应 10-100 的密度.
    /// </summary>
    public int? RollingDensity { get; set; }

    /// <summary>
    /// 弹幕字体大小缩放比例.
    /// </summary>
    public double? FontSizeScale { get; set; }

    /// <summary>
    /// 滚动区域比例（占画布高度的比例，范围 0-1）.
    /// </summary>
    public float? RollingAreaRatio { get; set; }

    /// <summary>
    /// 滚动速度基础值（1-10 的用户设置值，需要配合 scale 计算）.
    /// </summary>
    public double? RollingSpeedBase { get; set; }

    /// <summary>
    /// 文本不透明度 [0.0, 1.0].
    /// </summary>
    public double? TextOpacity { get; set; }

    /// <summary>
    /// 描边大小（像素）.
    /// </summary>
    public double? OutlineSize { get; set; }

    /// <summary>
    /// 边框颜色（用于调试模式）.
    /// </summary>
    public Color? BorderColor { get; set; }

    /// <summary>
    /// 字体名称.
    /// </summary>
    public string FontFamilyName { get; set; }

    #endregion

    /// <summary>
    /// 创建一个包含所有默认值的配置实例.
    /// </summary>
    public static DanmakuConfiguration CreateDefault()
    {
        return new DanmakuConfiguration
        {
            AutoControlDensity = true,
            IsTextBold = true,
            NoOverlapSubtitle = false,
            EnableParallelUpdate = true,
            RollingDensity = -1,
            FontSizeScale = 1.0,
            RollingAreaRatio = 0.8f,
            RollingSpeedBase = 5.0,
            TextOpacity = 1.0,
            OutlineSize = 2.0,
            BorderColor = Microsoft.UI.Colors.Blue,
            FontFamilyName = DefaultFontFamilyName,
        };
    }

    /// <summary>
    /// 创建配置的深拷贝.
    /// </summary>
    public DanmakuConfiguration Clone()
    {
        return new DanmakuConfiguration
        {
            AutoControlDensity = AutoControlDensity,
            IsTextBold = IsTextBold,
            NoOverlapSubtitle = NoOverlapSubtitle,
            EnableParallelUpdate = EnableParallelUpdate,
            RollingDensity = RollingDensity,
            FontSizeScale = FontSizeScale,
            RollingAreaRatio = RollingAreaRatio,
            RollingSpeedBase = RollingSpeedBase,
            TextOpacity = TextOpacity,
            OutlineSize = OutlineSize,
            BorderColor = BorderColor,
            FontFamilyName = FontFamilyName,
        };
    }
}
