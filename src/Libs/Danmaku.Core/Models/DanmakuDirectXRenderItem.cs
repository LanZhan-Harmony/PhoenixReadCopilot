using System.Diagnostics;
using Vortice.DirectWrite;

namespace Danmaku.Core.Models;

/// <summary>
/// DirectX 渲染弹幕项.
/// </summary>
[DebuggerDisplay("{Item.Text} at {FirstRenderTime.TotalMilliseconds}")]
internal sealed class DanmakuDirectXRenderItem : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DanmakuDirectXRenderItem"/> class.
    /// </summary>
    /// <param name="item">弹幕数据项.</param>
    internal DanmakuDirectXRenderItem(DanmakuItem item)
    {
        Initialize(item);
    }

    /// <summary>
    /// 初始化或重置 RenderItem（对象池复用）.
    /// </summary>
    internal void Initialize(DanmakuItem item)
    {
        if (string.IsNullOrEmpty(item.Text))
        {
            return;
        }

        Item = item; // 不再克隆，直接使用（由对象池管理生命周期）
        Id = (uint)Item.Text.GetHashCode(StringComparison.Ordinal);
        FontSize = Item.BaseFontSize > 0 ? Item.BaseFontSize : DanmakuItem.DefaultBaseFontSize;

        // 重置状态
        IsFirstRenderTimeSet = false;
        HasIncrementedCacheRef = false;
        FirstRenderTime = TimeSpan.Zero;
        Width = 0;
        Height = 0;
        X = 0;
        Y = 0;
        StartY = 0;
        NeedToReleaseYSlot = false;
        if (Segments is not null)
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                Segments[i]?.Dispose();
            }
            Segments.Clear();
            Segments = null;
        }
    }

    /// <summary>
    /// 弹幕数据项（由对象池管理，不再深拷贝）.
    /// </summary>
    internal DanmakuItem Item;

    /// <summary>
    /// 弹幕唯一标识符.
    /// </summary>
    internal uint Id;

    /// <summary>
    /// 字体大小（经过计算的实际大小）.
    /// </summary>
    internal float FontSize;

    /// <summary>
    /// 是否已设置首次渲染时间.
    /// </summary>
    internal volatile bool IsFirstRenderTimeSet;

    /// <summary>
    /// 是否已增加缓存引用计数.
    /// </summary>
    internal volatile bool HasIncrementedCacheRef;

    /// <summary>
    /// 首次渲染时间.
    /// </summary>
    internal TimeSpan FirstRenderTime;

    /// <summary>
    /// DirectWrite 文本布局（纯文本弹幕使用）.
    /// </summary>
    internal IDWriteTextLayout? TextLayout;

    /// <summary>
    /// 混合图文分段（含表情弹幕使用）.
    /// </summary>
    internal List<DanmakuSegment>? Segments;

    /// <summary>
    /// DirectWrite 文本格式.
    /// </summary>
    internal IDWriteTextFormat TextFormat;

    /// <summary>
    /// 宽度.
    /// </summary>
    internal volatile float Width;

    /// <summary>
    /// 高度.
    /// </summary>
    internal volatile float Height;

    /// <summary>
    /// X 坐标.
    /// </summary>
    internal volatile float X;

    /// <summary>
    /// Y 坐标.
    /// </summary>
    internal volatile float Y;

    /// <summary>
    /// 是否需要释放 Y 槽位.
    /// </summary>
    internal volatile bool NeedToReleaseYSlot;

    /// <summary>
    /// 起始 Y 坐标（分配的槽位 Y）.
    /// </summary>
    internal volatile float StartY;

    /// <inheritdoc/>
    public void Dispose()
    {
        TextLayout?.Dispose();
        TextLayout = null;

        if (Segments is not null)
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                Segments[i]?.Dispose();
            }
            Segments.Clear();
            Segments = null;
        }
    }
}
