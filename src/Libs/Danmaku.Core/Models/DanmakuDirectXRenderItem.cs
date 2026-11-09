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
    public DanmakuDirectXRenderItem(DanmakuItem item)
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
        Id = (uint)Item.Text.GetHashCode(System.StringComparison.Ordinal);
        FontSize = Item.BaseFontSize > 0 ? Item.BaseFontSize : DanmakuItem.DefaultBaseFontSize;

        // 重置状态
        IsFirstRenderTimeSet = false;
        HasIncrementedCacheRef = false;
        FirstRenderTime = TimeSpan.Zero;
        Width = 0;
        Height = 0;
        X = 0;
        Y = 0;
        NeedToReleaseYSlot = false;
        StartX = 0;
        StartY = 0;
        EndX = 0;
        EndY = 0;
        TranslationSpeedX = 0;
        TranslationSpeedY = 0;
    }

    /// <summary>
    /// 弹幕数据项（由对象池管理，不再深拷贝）.
    /// </summary>
    public DanmakuItem Item;

    /// <summary>
    /// 弹幕唯一标识符.
    /// </summary>
    public uint Id;

    /// <summary>
    /// 字体大小（经过计算的实际大小）.
    /// </summary>
    public float FontSize;

    /// <summary>
    /// 是否已设置首次渲染时间.
    /// </summary>
    public volatile bool IsFirstRenderTimeSet;

    /// <summary>
    /// 是否已增加缓存引用计数.
    /// </summary>
    public volatile bool HasIncrementedCacheRef;

    /// <summary>
    /// 首次渲染时间.
    /// </summary>
    public System.TimeSpan FirstRenderTime;

    /// <summary>
    /// DirectWrite 文本布局.
    /// </summary>
    public IDWriteTextLayout TextLayout;

    /// <summary>
    /// DirectWrite 文本格式.
    /// </summary>
    public IDWriteTextFormat TextFormat;

    /// <summary>
    /// 宽度.
    /// </summary>
    public volatile float Width;

    /// <summary>
    /// 高度.
    /// </summary>
    public volatile float Height;

    /// <summary>
    /// X 坐标.
    /// </summary>
    public volatile float X;

    /// <summary>
    /// Y 坐标.
    /// </summary>
#pragma warning disable CS0649 // 字段从未赋值
    public volatile float Y;
#pragma warning restore CS0649 // 字段从未赋值

    /// <summary>
    /// 是否需要释放 Y 槽位.
    /// </summary>
    public volatile bool NeedToReleaseYSlot;

    /// <summary>
    /// 起始 X 坐标（用于动画）.
    /// </summary>
    public float StartX;

    /// <summary>
    /// 起始 Y 坐标（用于动画）.
    /// </summary>
    public float StartY;

    /// <summary>
    /// 结束 X 坐标（用于动画）.
    /// </summary>
    public float EndX;

    /// <summary>
    /// 结束 Y 坐标（用于动画）.
    /// </summary>
    public float EndY;

    /// <summary>
    /// X 方向平移速度.
    /// </summary>
    public float TranslationSpeedX;

    /// <summary>
    /// Y 方向平移速度.
    /// </summary>
    public float TranslationSpeedY;

    /// <inheritdoc/>
    public void Dispose()
    {
        TextLayout?.Dispose();
        // TextFormat 是缓存的，不要在这里 Dispose
    }
}
