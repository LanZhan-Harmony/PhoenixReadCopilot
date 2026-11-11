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

    /// <summary>
    /// 顶部/底部停留持续时间（毫秒）.
    /// </summary>
    public float DurationMs;

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
            DurationMs = DurationMs,
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
        DurationMs = source.DurationMs;
    }
}
#pragma warning restore CA1051 // 不要声明可见实例字段
