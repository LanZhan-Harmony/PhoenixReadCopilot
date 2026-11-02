using Danmaku.Core.Enums;
using Microsoft.UI;
using Windows.UI;

namespace Danmaku.Core.Models;

/// <summary>
/// 弹幕数据项.
/// </summary>
#pragma warning disable CA1051 // 不要声明可见实例字段
public class DanmakuItem
{
    /// <summary>
    /// 默认基础字体大小.
    /// </summary>
#pragma warning disable CA2211 // 非常量字段应当不可见
    public static float DefaultBaseFontSize = 22;
#pragma warning restore CA2211 // 非常量字段应当不可见

    /// <summary>
    /// 弹幕唯一标识符，用于排序具有相同 StartMs 的弹幕.
    /// </summary>
    public string Id;

    /// <summary>
    /// 弹幕开始显示的时间（毫秒）.
    /// </summary>
    public uint StartMs;

    /// <summary>
    /// 是否有边框.
    /// </summary>
    public bool HasBorder;

    /// <summary>
    /// 是否有描边.
    /// </summary>
    public bool HasOutline = true;

    /// <summary>
    /// 是否允许密度控制.
    /// </summary>
    public bool AllowDensityControl = true;

    /// <summary>
    /// 是否为实时弹幕.
    /// </summary>
    public bool IsRealtime;

    /// <summary>
    /// 基础字体大小.
    /// </summary>
    public float BaseFontSize;

    /// <summary>
    /// 描边大小.
    /// </summary>
    public float OutlineSize = 2f;

    /// <summary>
    /// 字体家族名称.
    /// </summary>
    public string FontFamilyName;

    /// <summary>
    /// 弹幕文本内容.
    /// </summary>
    public string Text;

    /// <summary>
    /// 是否加粗.
    /// </summary>
    public bool? IsBold;

    /// <summary>
    /// 弹幕模式.
    /// </summary>
    public DanmakuMode Mode;

    /// <summary>
    /// 文本颜色.
    /// </summary>
    public Color TextColor = Colors.White;

    /// <summary>
    /// 描边颜色.
    /// </summary>
    public Color OutlineColor = Colors.Black;

    #region For Advanced mode

    /// <summary>
    /// 起始 X 坐标（高级模式）.
    /// </summary>
    public float StartX;

    /// <summary>
    /// 起始 Y 坐标（高级模式）.
    /// </summary>
    public float StartY;

    /// <summary>
    /// 结束 X 坐标（高级模式）.
    /// </summary>
    public float EndX;

    /// <summary>
    /// 结束 Y 坐标（高级模式）.
    /// </summary>
    public float EndY;

    /// <summary>
    /// 左边距.
    /// </summary>
    public int MarginLeft;

    /// <summary>
    /// 右边距.
    /// </summary>
    public int MarginRight;

    /// <summary>
    /// 下边距.
    /// </summary>
    public int MarginBottom;

    /// <summary>
    /// 对齐模式.
    /// </summary>
    public DanmakuAlignmentMode AlignmentMode = DanmakuAlignmentMode.Default;

    /// <summary>
    /// 锚点模式.
    /// </summary>
    public DanmakuAlignmentMode AnchorMode = DanmakuAlignmentMode.UpperLeft;

    /// <summary>
    /// 起始透明度.
    /// </summary>
    public byte StartAlpha;

    /// <summary>
    /// 结束透明度.
    /// </summary>
    public byte EndAlpha;

    /// <summary>
    /// 持续时间（毫秒）.
    /// </summary>
    public ulong DurationMs;

    /// <summary>
    /// 平移持续时间（毫秒）.
    /// </summary>
    public ulong TranslationDurationMs;

    /// <summary>
    /// 平移延迟时间（毫秒）.
    /// </summary>
    public ulong TranslationDelayMs;

    /// <summary>
    /// 透明度变化持续时间（毫秒）.
    /// </summary>
    public ulong AlphaDurationMs;

    /// <summary>
    /// 透明度变化延迟时间（毫秒）.
    /// </summary>
    public ulong AlphaDelayMs;

    /// <summary>
    /// Z 轴旋转角度（度数）.
    /// </summary>
    public float RotateZ;

    /// <summary>
    /// Y 轴旋转角度（度数）.
    /// </summary>
    public float RotateY;

    /// <summary>
    /// 是否保持定义的字体大小.
    /// </summary>
    public bool KeepDefinedFontSize;

    #endregion

    /// <summary>
    /// 创建当前弹幕项的深拷贝.
    /// </summary>
    /// <returns>新的弹幕项实例.</returns>
    public DanmakuItem Clone()
    {
        return new DanmakuItem
        {
            Id = Id,
            StartMs = StartMs,
            HasBorder = HasBorder,
            HasOutline = HasOutline,
            AllowDensityControl = AllowDensityControl,
            IsRealtime = IsRealtime,
            BaseFontSize = BaseFontSize,
            OutlineSize = OutlineSize,
            FontFamilyName = FontFamilyName,
            Text = Text,
            IsBold = IsBold,
            Mode = Mode,
            TextColor = TextColor,
            OutlineColor = OutlineColor,
            StartX = StartX,
            StartY = StartY,
            EndX = EndX,
            EndY = EndY,
            MarginLeft = MarginLeft,
            MarginRight = MarginRight,
            MarginBottom = MarginBottom,
            AlignmentMode = AlignmentMode,
            AnchorMode = AnchorMode,
            StartAlpha = StartAlpha,
            EndAlpha = EndAlpha,
            DurationMs = DurationMs,
            TranslationDurationMs = TranslationDurationMs,
            TranslationDelayMs = TranslationDelayMs,
            AlphaDurationMs = AlphaDurationMs,
            AlphaDelayMs = AlphaDelayMs,
            RotateZ = RotateZ,
            RotateY = RotateY,
            KeepDefinedFontSize = KeepDefinedFontSize,
        };
    }

    /// <summary>
    /// 从另一个 DanmakuItem 复制所有属性（对象池复用）.
    /// </summary>
    public void CopyFrom(DanmakuItem source)
    {
        Id = source.Id;
        StartMs = source.StartMs;
        HasBorder = source.HasBorder;
        HasOutline = source.HasOutline;
        AllowDensityControl = source.AllowDensityControl;
        IsRealtime = source.IsRealtime;
        BaseFontSize = source.BaseFontSize;
        OutlineSize = source.OutlineSize;
        FontFamilyName = source.FontFamilyName;
        Text = source.Text;
        IsBold = source.IsBold;
        Mode = source.Mode;
        TextColor = source.TextColor;
        OutlineColor = source.OutlineColor;
        StartX = source.StartX;
        StartY = source.StartY;
        EndX = source.EndX;
        EndY = source.EndY;
        MarginLeft = source.MarginLeft;
        MarginRight = source.MarginRight;
        MarginBottom = source.MarginBottom;
        AlignmentMode = source.AlignmentMode;
        AnchorMode = source.AnchorMode;
        StartAlpha = source.StartAlpha;
        EndAlpha = source.EndAlpha;
        DurationMs = source.DurationMs;
        TranslationDurationMs = source.TranslationDurationMs;
        TranslationDelayMs = source.TranslationDelayMs;
        AlphaDurationMs = source.AlphaDurationMs;
        AlphaDelayMs = source.AlphaDelayMs;
        RotateZ = source.RotateZ;
        RotateY = source.RotateY;
        KeepDefinedFontSize = source.KeepDefinedFontSize;
    }
}
#pragma warning restore CA1051 // 不要声明可见实例字段
