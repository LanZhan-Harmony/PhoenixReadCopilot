using System.Diagnostics;

namespace Danmaku.Core.Models;

/// <summary>
/// 渲染图层.
/// </summary>
[DebuggerDisplay("IsEnabled:{IsEnabled} Count:{RenderList.Count}")]
internal sealed class RenderLayer
{
    private readonly uint _layerId;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderLayer"/> class.
    /// </summary>
    /// <param name="layerId">图层 ID (0 为最低层，数值越大层级越高).</param>
    /// <param name="requireStrictRenderOrder">是否要求严格的渲染顺序.</param>
    public RenderLayer(uint layerId, bool requireStrictRenderOrder)
    {
        _layerId = layerId;
        RequireStrictRenderOrder = requireStrictRenderOrder;
    }

    /// <summary>
    /// 渲染项列表.
    /// </summary>
    public readonly List<DanmakuDirectXRenderItem> RenderList = [];

    /// <summary>
    /// Y 轴槽位管理器.
    /// </summary>
    public readonly DanmakuYSlotManager YSlotManager = new(0);

    /// <summary>
    /// 是否要求严格的渲染顺序.
    /// </summary>
    public readonly bool RequireStrictRenderOrder;

    /// <summary>
    /// 是否启用该图层.
    /// </summary>
    public bool IsEnabled = true;

    /// <summary>
    /// 是否为字幕图层.
    /// </summary>
    public bool IsSubtitleLayer { get; private set; }

    /// <summary>
    /// 更新 Y 轴槽位管理器的长度.
    /// </summary>
    /// <param name="newLength">新长度.</param>
    /// <param name="rollingLayerAreaRatio">滚动图层区域比例.</param>
    public void UpdateYSlotManagerLength(uint newLength, float rollingLayerAreaRatio)
    {
        if (
            _layerId == DanmakuDefaultLayerDef.RollingLayerId
            || _layerId == DanmakuDefaultLayerDef.ReverseRollingLayerId
        )
        {
            YSlotManager.UpdateLength((uint)Math.Floor(newLength * rollingLayerAreaRatio));
        }
        else if (_layerId == DanmakuDefaultLayerDef.TopLayerId)
        {
            YSlotManager.UpdateLength(
                (uint)Math.Floor(newLength * System.Math.Min(rollingLayerAreaRatio, 0.75))
            );
        }
        else if (_layerId == DanmakuDefaultLayerDef.BottomLayerId)
        {
            YSlotManager.UpdateLength(newLength / 2);
        }
        else
        {
            YSlotManager.UpdateLength(newLength);
        }
    }

    /// <summary>
    /// 清空渲染列表（线程安全）.
    /// </summary>
    /// <param name="objectPool">对象池管理器（可选，用于归还对象）.</param>
    public void Clear(ObjectPoolManager objectPool = null)
    {
        lock (RenderList)
        {
            if (objectPool != null)
            {
                // 🔧 修复：清理纹理缓存并归还所有对象到对象池
                for (int i = 0; i < RenderList.Count; i++)
                {
                    objectPool.ReturnRenderItemAndClearTexture(RenderList[i]);
                }
            }
            else
            {
                // 回退方案：直接释放
                for (int i = 0; i < RenderList.Count; i++)
                {
                    RenderList[i].Dispose();
                }
            }

            RenderList.Clear();
        }

        YSlotManager.Clear();
    }

    /// <summary>
    /// 设置是否为字幕图层.
    /// </summary>
    /// <param name="isSubtitleLayer">是否为字幕图层.</param>
    public void SetSubtitleLayer(bool isSubtitleLayer)
    {
        IsSubtitleLayer = isSubtitleLayer;
    }
}
