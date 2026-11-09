using System.Collections.Concurrent;
using Danmaku.Core.Enums;
using Danmaku.Core.Models;
using Microsoft.Extensions.ObjectPool;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace Danmaku.Core;

/// <summary>
/// 对象池管理器，使用 Microsoft.Extensions.ObjectPool.
/// </summary>
internal sealed class ObjectPoolManager : IDisposable
{
    private bool _isDisposed;

    // 使用 Microsoft.Extensions.ObjectPool
    private readonly ObjectPool<DanmakuItem> _danmakuItemPool;
    private readonly ObjectPool<DanmakuDirectXRenderItem> _renderItemPool;

    // 纹理缓存 - 使用 ConcurrentDictionary（纹理不适合对象池）
    private readonly ConcurrentDictionary<TextureCacheKey, TexturePoolEntry> _textureCache;
    private readonly ConcurrentDictionary<TextureCacheKey, IDWriteTextLayout> _textLayoutCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPoolManager"/> class.
    /// </summary>
    public ObjectPoolManager()
    {
        // 创建 DanmakuItem 对象池（使用默认策略）
        DefaultObjectPoolProvider danmakuItemPolicy = new() { MaximumRetained = 2000 };
        _danmakuItemPool = danmakuItemPolicy.Create(new DanmakuItemPoolPolicy());

        // 创建 RenderItem 对象池（使用默认策略）
        DefaultObjectPoolProvider renderItemPolicy = new() { MaximumRetained = 2000 };
        _renderItemPool = renderItemPolicy.Create(new RenderItemPoolPolicy());

        // 纹理缓存（不使用对象池，因为纹理生命周期复杂）
        _textureCache = new ConcurrentDictionary<TextureCacheKey, TexturePoolEntry>();
        _textLayoutCache = new ConcurrentDictionary<TextureCacheKey, IDWriteTextLayout>();
    }

    /// <summary>
    /// 纹理缓存条目.
    /// </summary>
    private sealed class TexturePoolEntry
    {
        public ID2D1Bitmap1? Bitmap;
        public float Width;
        public float Height;
        public int RefCount; // 引用计数
    }

    #region DanmakuItem 对象池

    /// <summary>
    /// DanmakuItem 对象池策略.
    /// </summary>
    private sealed class DanmakuItemPoolPolicy : IPooledObjectPolicy<DanmakuItem>
    {
        public DanmakuItem Create()
        {
            return new DanmakuItem();
        }

        public bool Return(DanmakuItem obj)
        {
            // 重置对象状态
            if (obj == null)
            {
                return false;
            }

            obj.Id = null;
            obj.StartMs = 0;
            obj.HasBorder = false;
            obj.HasOutline = true;
            obj.AllowDensityControl = true;
            obj.IsRealtime = false;
            obj.BaseFontSize = 0;
            obj.OutlineSize = 2f;
            obj.FontFamilyName = null;
            obj.Text = null;
            obj.IsBold = null;
            obj.Mode = DanmakuMode.Rolling;
            obj.TextColor = Microsoft.UI.Colors.White;
            obj.OutlineColor = Microsoft.UI.Colors.Black;
            obj.StartX = 0;
            obj.StartY = 0;
            obj.EndX = 0;
            obj.EndY = 0;
            obj.MarginLeft = 0;
            obj.MarginRight = 0;
            obj.MarginBottom = 0;
            obj.AlignmentMode = DanmakuAlignmentMode.Default;
            obj.AnchorMode = DanmakuAlignmentMode.UpperLeft;
            obj.StartAlpha = 0;
            obj.EndAlpha = 0;
            obj.DurationMs = 0;
            obj.TranslationDurationMs = 0;
            obj.TranslationDelayMs = 0;
            obj.AlphaDurationMs = 0;
            obj.AlphaDelayMs = 0;
            obj.RotateZ = 0;
            obj.RotateY = 0;
            obj.KeepDefinedFontSize = false;

            return true;
        }
    }

    /// <summary>
    /// 从对象池获取 DanmakuItem.
    /// </summary>
    public DanmakuItem RentDanmakuItem()
    {
        return _danmakuItemPool.Get();
    }

    /// <summary>
    /// 归还 DanmakuItem 到对象池.
    /// </summary>
    public void ReturnDanmakuItem(DanmakuItem? item)
    {
        if (item != null)
        {
            _danmakuItemPool.Return(item);
        }
    }

    #endregion

    #region DanmakuDirectXRenderItem 对象池

    /// <summary>
    /// RenderItem 对象池策略.
    /// </summary>
    private sealed class RenderItemPoolPolicy : IPooledObjectPolicy<DanmakuDirectXRenderItem>
    {
        public DanmakuDirectXRenderItem Create()
        {
            // 创建一个空的 RenderItem（需要后续 Initialize）
            return new DanmakuDirectXRenderItem(new DanmakuItem());
        }

        public bool Return(DanmakuDirectXRenderItem obj)
        {
            if (obj == null)
            {
                return false;
            }

            // 🔧 修复：释放 TextLayout
            // 每个 RenderItem 都有自己的 TextLayout，必须释放
            // 注意：缓存中的 TextLayout 由 MarkTextureUnused 负责释放
            obj.TextLayout?.Dispose();
            obj.TextLayout = null;
            obj.TextFormat = null;
            obj.Item = null;

            return true;
        }
    }

    /// <summary>
    /// 从对象池获取 DanmakuDirectXRenderItem.
    /// </summary>
    public DanmakuDirectXRenderItem RentRenderItem(DanmakuItem item)
    {
        DanmakuDirectXRenderItem renderItem = _renderItemPool.Get();
        renderItem.Initialize(item);
        return renderItem;
    }

    /// <summary>
    /// 归还 DanmakuDirectXRenderItem 到对象池.
    /// </summary>
    public void ReturnRenderItem(DanmakuDirectXRenderItem? item)
    {
        if (item != null)
        {
            _renderItemPool.Return(item);
        }
    }

    /// <summary>
    /// 归还 DanmakuDirectXRenderItem 到对象池，并清理对应的纹理缓存.
    /// </summary>
    /// <param name="item">要归还的渲染项.</param>
    public void ReturnRenderItemAndClearTexture(DanmakuDirectXRenderItem? item)
    {
        if (item == null)
        {
            return;
        }

        // 先清理纹理缓存
        TextureCacheKey key = new(
            item.Item.Text,
            item.Item.TextColor,
            item.Item.OutlineColor,
            item.FontSize,
            item.Item.HasOutline
        );

        MarkTextureUnused(key);

        // 再归还 RenderItem
        _renderItemPool.Return(item);
    }

    #endregion

    #region 纹理缓存管理

    /// <summary>
    /// 存储纹理到缓存（无引用计数模式）.
    /// </summary>
    public void PreRenderTexture(
        TextureCacheKey key,
        ID2D1Bitmap1 bitmap,
        float width,
        float height,
        IDWriteTextLayout textLayout
    )
    {
        // 🔧 简化：弹幕已去重，不需要引用计数
        // 每个 Bitmap 只被一个弹幕使用，消失时立即释放
        _textureCache.TryAdd(
            key,
            new TexturePoolEntry
            {
                Bitmap = bitmap,
                Width = width,
                Height = height,
                RefCount = 0, // 不再使用 RefCount
            }
        );

        // 🔧 修复：不再缓存 TextLayout
        // 原因：每个弹幕都应该有自己的 TextLayout，不应该共享
        // TextLayout 会在 ReturnRenderItem 时被 Dispose
        // _textLayoutCache.TryAdd(key, textLayout);
    }

    /// <summary>
    /// 从缓存获取纹理（简化版：无引用计数）.
    /// </summary>
    public bool TryGetTexture(
        TextureCacheKey key,
        out ID2D1Bitmap1? bitmap,
        out float width,
        out float height
    )
    {
        if (_textureCache.TryGetValue(key, out TexturePoolEntry? entry) && entry.Bitmap != null)
        {
            bitmap = entry.Bitmap;
            width = entry.Width;
            height = entry.Height;

            // 🔧 移除引用计数：弹幕已去重，每个 Bitmap 只被一个弹幕使用
            // 不需要 RefCount，弹幕消失时直接释放

            return true;
        }

        bitmap = null;
        width = 0;
        height = 0;
        return false;
    }

    /// <summary>
    /// 从缓存获取 TextLayout（无锁，高性能）.
    /// </summary>
    public bool TryGetTextLayout(TextureCacheKey key, out IDWriteTextLayout? textLayout)
    {
        return _textLayoutCache.TryGetValue(key, out textLayout);
    }

    /// <summary>
    /// 标记纹理为未使用状态，并立即释放资源（无缓存模式）.
    /// </summary>
    public void MarkTextureUnused(TextureCacheKey key)
    {
        // � 优化：弹幕已去重，不需要缓存共享
        // 弹幕消失后立即释放 Bitmap，避免进入 Gen2
        if (_textureCache.TryRemove(key, out TexturePoolEntry? entry))
        {
            entry.Bitmap?.Dispose();
            entry.Bitmap = null;
        }
    }

    /// <summary>
    /// 获取纹理缓存统计信息.
    /// </summary>
    public (int Total, int InUse) GetTextureStats()
    {
        int total = _textureCache.Count;
        int inUse = _textureCache.Values.Count(e => e.RefCount > 0);
        return (total, inUse);
    }

    /// <summary>
    /// 获取对象池统计信息（注意：ObjectPool 不提供 Count）.
    /// </summary>
    public (int TextureCache, int TextLayoutCache) GetPoolStats()
    {
        return (_textureCache.Count, _textLayoutCache.Count);
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // 清空纹理缓存
        foreach (TexturePoolEntry entry in _textureCache.Values)
        {
            entry.Bitmap?.Dispose();
        }
        _textureCache.Clear();

        // 清空 TextLayout 缓存
        foreach (IDWriteTextLayout layout in _textLayoutCache.Values)
        {
            layout?.Dispose();
        }
        _textLayoutCache.Clear();

        // 注意：Microsoft.Extensions.ObjectPool 会自动管理池中的对象
        // 不需要手动清理
    }
}
