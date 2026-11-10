using System.Numerics;
using Danmaku.Core.Enums;
using Danmaku.Core.Models;
using SharpGen.Runtime;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Windows.UI;

namespace Danmaku.Core;

/// <summary>
/// DanmakuDirectXRender 的辅助方法部分.
/// 包含资源创建、缓存管理和绘制辅助方法.
/// </summary>
internal partial class DanmakuDirectXRender
{
    #region 资源创建和缓存管理

    /// <summary>
    /// 获取或创建文本格式（使用缓存提升性能）.
    /// </summary>
    /// <param name="fontFamily">字体名称.</param>
    /// <param name="fontSize">字体大小.</param>
    /// <param name="isBold">是否粗体.</param>
    /// <returns>DirectWrite 文本格式.</returns>
    private IDWriteTextFormat? GetOrCreateTextFormat(string fontFamily, float fontSize, bool isBold)
    {
        var key = $"{fontFamily}_{fontSize:F1}_{isBold}";

        lock (_resourceManager.CacheLock)
        {
            if (
                !_resourceManager.TextFormatCache.TryGetValue(
                    key,
                    out IDWriteTextFormat? textFormat
                )
            )
            {
                textFormat = _resourceManager.DWriteFactory.CreateTextFormat(
                    fontFamily,
                    isBold ? FontWeight.Bold : FontWeight.Normal,
                    Vortice.DirectWrite.FontStyle.Normal,
                    FontStretch.Normal,
                    fontSize
                );

                textFormat.WordWrapping = Vortice.DirectWrite.WordWrapping.NoWrap;

                // 限制缓存大小
                if (_resourceManager.TextFormatCache.Count < 100)
                {
                    _resourceManager.TextFormatCache[key] = textFormat;
                }
            }

            return textFormat;
        }
    }

    /// <summary>
    /// 获取或创建画刷（使用缓存提升性能）.
    /// </summary>
    /// <param name="color">颜色.</param>
    /// <returns>D2D1 纯色画刷.</returns>
    private ID2D1SolidColorBrush? GetOrCreateBrush(Color color)
    {
        var colorKey = (uint)(color.A << 24 | color.R << 16 | color.G << 8 | color.B);

        lock (_resourceManager.CacheLock)
        {
            if (!_resourceManager.BrushCache.TryGetValue(colorKey, out ID2D1SolidColorBrush? brush))
            {
                brush = _resourceManager.D2DContext.CreateSolidColorBrush(
                    new Vortice.Mathematics.Color(color.R, color.G, color.B, color.A)
                );

                // 限制缓存大小
                if (_resourceManager.BrushCache.Count < 200)
                {
                    _resourceManager.BrushCache[colorKey] = brush;
                }
            }

            return brush;
        }
    }

    /// <summary>
    /// 创建颜色画刷（不使用缓存）.
    /// </summary>
    /// <param name="color">颜色.</param>
    /// <returns>D2D1 纯色画刷.</returns>
    private ID2D1SolidColorBrush CreateColorBrush(Windows.UI.Color color)
    {
        return _resourceManager.D2DContext.CreateSolidColorBrush(
            new Vortice.Mathematics.Color(
                (byte)color.R,
                (byte)color.G,
                (byte)color.B,
                (byte)color.A
            )
        );
    }

    /// <summary>
    /// 创建文本的位图纹理（离屏渲染）.
    /// </summary>
    /// <param name="renderItem">要渲染的弹幕项.</param>
    /// <returns>创建的位图，如果失败则返回 null.</returns>
    private ID2D1Bitmap1? CreateTextBitmap(DanmakuDirectXRenderItem renderItem)
    {
        try
        {
            if (renderItem.Item is null)
            {
                return default;
            }

            // 计算位图尺寸，需要为描边预留额外空间
            var outlinePadding = renderItem.Item.HasOutline
                ? (float)_configState.OutlineSize * 2
                : 0;
            var width = (int)Math.Ceiling(renderItem.Width + outlinePadding);
            var height = (int)Math.Ceiling(renderItem.Height + outlinePadding);

            if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
            {
                return null;
            }

            // 使用 CompatibleRenderTarget 而不是直接创建位图
            // 这样可以避免 BeginDraw/EndDraw 嵌套问题
            ID2D1BitmapRenderTarget compatibleTarget =
                _resourceManager.D2DContext.CreateCompatibleRenderTarget(
                    new Vortice.Mathematics.SizeI(width, height)
                );

            compatibleTarget.BeginDraw();
            compatibleTarget.Clear(_clearColor); // 🚀 优化：使用预分配的 Color4

            // 计算描边偏移（让文本居中，避免边缘截断）
            var offset = renderItem.Item.HasOutline ? (float)_configState.OutlineSize : 0;

            // 绘制描边（创建临时 Brush，使用完全不透明的颜色）
            if (renderItem.Item.HasOutline && _configState.OutlineSize > 0)
            {
                Color outlineColor =
                    renderItem.Item.TextColor.R
                        + renderItem.Item.TextColor.G
                        + renderItem.Item.TextColor.B
                    < 0x20
                        ? Microsoft.UI.Colors.White
                        : renderItem.Item.OutlineColor;
                // 使用完全不透明（Alpha = 255），透明度将在绘制位图时应用
                outlineColor.A = 255;

                using ID2D1SolidColorBrush outlineBrush = compatibleTarget.CreateSolidColorBrush(
                    new Vortice.Mathematics.Color4(
                        outlineColor.R / 255f,
                        outlineColor.G / 255f,
                        outlineColor.B / 255f,
                        1.0f
                    )
                ); // Alpha = 1.0 (完全不透明)

                var strokeWidth = (float)_configState.OutlineSize;
                for (int i = 0; i < _outlineOffsets.Length; i++)
                {
                    Vector2 outlineOffset = _outlineOffsets[i] * strokeWidth;
                    _reusableVector2.X = offset + outlineOffset.X;
                    _reusableVector2.Y = offset + outlineOffset.Y;
                    compatibleTarget.DrawTextLayout(
                        _reusableVector2,
                        renderItem.TextLayout,
                        outlineBrush,
                        DrawTextOptions.EnableColorFont
                    ); // 启用彩色字体支持
                }
            }

            // 绘制主文本（创建临时 Brush，使用完全不透明的颜色）
            using ID2D1SolidColorBrush textBrush = compatibleTarget.CreateSolidColorBrush(
                new Vortice.Mathematics.Color4(
                    renderItem.Item.TextColor.R / 255f,
                    renderItem.Item.TextColor.G / 255f,
                    renderItem.Item.TextColor.B / 255f,
                    1.0f
                )
            ); // Alpha = 1.0 (完全不透明，透明度将在绘制位图时应用)

            _reusableVector2.X = offset;
            _reusableVector2.Y = offset;
            compatibleTarget.DrawTextLayout(
                _reusableVector2,
                renderItem.TextLayout,
                textBrush,
                DrawTextOptions.EnableColorFont
            ); // 启用彩色字体支持（关键：显示彩色 Emoji）

            Result result = compatibleTarget.EndDraw(out _, out _);
            if (result.Failure)
            {
                Logger.Log($"CreateTextBitmap EndDraw failed: {result}");
                compatibleTarget.Dispose();
                return null;
            }

            // 获取位图（CompatibleRenderTarget.Bitmap 返回 ID2D1Bitmap，需要转换）
            ID2D1Bitmap bitmap = compatibleTarget.Bitmap;

            // 转换为 ID2D1Bitmap1（通过 QueryInterface）
            ID2D1Bitmap1 bitmap1 = bitmap.QueryInterface<ID2D1Bitmap1>();

            // 释放原始位图引用（避免内存泄漏）
            bitmap.Dispose();
            compatibleTarget.Dispose();

            return bitmap1;
        }
        catch (Exception ex)
        {
            Logger.Log($"CreateTextBitmap failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 🚀 GC 优化：生成纹理缓存的键（零分配，使用结构体）.
    /// </summary>
    /// <param name="renderItem">渲染项.</param>
    /// <returns>缓存键结构体.</returns>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining
    )]
    private static TextureCacheKey GetTextureCacheKey(DanmakuDirectXRenderItem renderItem)
    {
        // 🚀 零分配：直接构造结构体，不使用 StringBuilder
        return new TextureCacheKey(
            renderItem.Item.Text,
            renderItem.Item.TextColor,
            renderItem.Item.OutlineColor,
            renderItem.FontSize,
            renderItem.Item.HasOutline
        );
    }

    #endregion

    #region 绘制辅助方法

    /// <summary>
    /// 绘制弹幕文本（主入口，优先使用纹理缓存）.
    /// </summary>
    /// <param name="renderItem">要绘制的渲染项.</param>
    /// <param name="x">X 坐标.</param>
    /// <param name="y">Y 坐标.</param>
    private void DrawDanmakuText(DanmakuDirectXRenderItem renderItem, float x, float y)
    {
        if (renderItem.TextLayout == null)
        {
            Logger.Log($"TextLayout is null for: {renderItem.Item.Text}");
            return;
        }

        try
        {
            // 如果禁用纹理缓存，直接绘制
            if (!_enableTextureCache)
            {
                DrawDanmakuTextDirect(renderItem, x, y);
                return;
            }

            // 尝试使用纹理缓存（快速路径）
            if (TryDrawCachedTexture(renderItem, x, y))
            {
                return;
            }

            // 缓存未命中：光栅化到纹理并缓存
            DrawAndCacheTexture(renderItem, x, y);
        }
        catch (Exception ex)
        {
            Logger.Log(
                $"DrawDanmakuText error for '{renderItem.Item.Text}' at ({x},{y}): {ex.Message}"
            );

            // 回退到直接绘制（用于错误恢复）
            try
            {
                DrawDanmakuTextDirect(renderItem, x, y);
            }
            catch (Exception fallbackEx)
            {
                Logger.Log($"Fallback drawing failed: {fallbackEx.Message}");
            }
        }
    }

    /// <summary>
    /// 直接绘制弹幕文本（回退方法，仅用于错误恢复）.
    /// </summary>
    /// <param name="renderItem">要绘制的渲染项.</param>
    /// <param name="x">X 坐标.</param>
    /// <param name="y">Y 坐标.</param>
    private void DrawDanmakuTextDirect(DanmakuDirectXRenderItem renderItem, float x, float y)
    {
        Vortice.Mathematics.Rect rect = new(x, y, renderItem.Width, renderItem.Height);

        // Draw border if needed (for debugging)
        if (renderItem.Item.HasBorder || DebugMode)
        {
            ID2D1SolidColorBrush? borderBrush = GetOrCreateBrush(_configState.BorderColor);
            if (borderBrush != null)
            {
                _resourceManager.D2DContext.DrawRectangle(rect, borderBrush, 2f);
            }
        }

        // Draw outline (优化版本 - 4方向，使用预计算偏移)
        if (renderItem.Item.HasOutline && _configState.OutlineSize > 0)
        {
            Color outlineColor =
                renderItem.Item.TextColor.R
                    + renderItem.Item.TextColor.G
                    + renderItem.Item.TextColor.B
                < 0x20
                    ? Microsoft.UI.Colors.White
                    : renderItem.Item.OutlineColor;

            ID2D1SolidColorBrush? outlineBrush = GetOrCreateBrush(outlineColor);
            var strokeWidth = (float)_configState.OutlineSize;

            // 使用预计算的偏移量（🚀 优化：重用 Vector2，避免每次分配）
            for (int i = 0; i < _outlineOffsets.Length; i++)
            {
                Vector2 offset = _outlineOffsets[i] * strokeWidth;
                _reusableVector2.X = x + offset.X;
                _reusableVector2.Y = y + offset.Y;
                _resourceManager.D2DContext.DrawTextLayout(
                    _reusableVector2,
                    renderItem.TextLayout,
                    outlineBrush,
                    DrawTextOptions.EnableColorFont
                ); // 启用彩色字体支持
            }
        }

        // Draw main text with color font support (for emoji)
        ID2D1SolidColorBrush? textBrush = GetOrCreateBrush(renderItem.Item.TextColor);
        _reusableVector2.X = x;
        _reusableVector2.Y = y;
        _resourceManager.D2DContext.DrawTextLayout(
            _reusableVector2,
            renderItem.TextLayout,
            textBrush,
            DrawTextOptions.EnableColorFont
        ); // 启用彩色字体支持（关键：显示彩色 Emoji）
    }

    /// <summary>
    /// 尝试使用对象池中的纹理绘制（快速路径）.
    /// </summary>
    /// <param name="renderItem">要绘制的渲染项.</param>
    /// <param name="x">X 坐标.</param>
    /// <param name="y">Y 坐标.</param>
    /// <returns>如果成功使用缓存绘制返回 true，否则返回 false.</returns>
    private bool TryDrawCachedTexture(DanmakuDirectXRenderItem renderItem, float x, float y)
    {
        TextureCacheKey cacheKey = GetTextureCacheKey(renderItem);

        // 从对象池获取纹理
        if (
            _resourceManager.ObjectPool.TryGetTexture(
                cacheKey,
                out ID2D1Bitmap1? cachedBitmap,
                out var width,
                out var height
            )
        )
        {
            if (cachedBitmap == null)
            {
                return false;
            }

            // 保存当前变换矩阵
            Matrix3x2 prevTransform = _resourceManager.D2DContext.Transform;

            // 计算绘制位置（需要减去描边偏移）
            var offset = renderItem.Item.HasOutline ? (float)_configState.OutlineSize : 0;

            // 设置到目标位置（🚀 优化：重用 Matrix3x2，避免分配）
            _reusableMatrix.M11 = 1;
            _reusableMatrix.M12 = 0;
            _reusableMatrix.M21 = 0;
            _reusableMatrix.M22 = 1;
            _reusableMatrix.M31 = x - offset;
            _reusableMatrix.M32 = y - offset;
            _resourceManager.D2DContext.Transform = _reusableMatrix * prevTransform;

            // 计算透明度（Advanced 和 Subtitle 模式使用完全不透明）
            float opacity =
                (
                    renderItem.Item.Mode == DanmakuMode.Advanced
                    || renderItem.Item.Mode == DanmakuMode.Subtitle
                )
                    ? 1.0f
                    : (float)_configState.TextOpacity;

            // 直接绘制缓存的位图（应用真正的透明度！）
            _resourceManager.D2DContext.DrawBitmap(cachedBitmap, opacity, InterpolationMode.Linear);

            // 恢复变换矩阵
            _resourceManager.D2DContext.Transform = prevTransform;

            return true;
        }

        return false;
    }

    /// <summary>
    /// 绘制文本（从对象池获取或创建并缓存）.
    /// </summary>
    /// <param name="renderItem">要绘制的渲染项.</param>
    /// <param name="x">X 坐标.</param>
    /// <param name="y">Y 坐标.</param>
    private void DrawAndCacheTexture(DanmakuDirectXRenderItem renderItem, float x, float y)
    {
        TextureCacheKey cacheKey = GetTextureCacheKey(renderItem);

        // 尝试从对象池获取
        if (
            _resourceManager.ObjectPool.TryGetTexture(
                cacheKey,
                out ID2D1Bitmap1? cachedBitmap,
                out var width,
                out var height
            )
            && cachedBitmap != null
        )
        {
            // 找到了纹理，直接绘制（TryGetTexture 已经增加引用计数）
            DrawBitmapAtPosition(cachedBitmap, renderItem, x, y);
        }
        else
        {
            // 纹理不存在，创建并缓存到对象池
            ID2D1Bitmap1? newBitmap = CreateTextBitmap(renderItem);
            if (newBitmap != null)
            {
                // 添加到对象池（PreRenderTexture 会将 RefCount 初始化为 1）
                _resourceManager.ObjectPool.PreRenderTexture(
                    cacheKey,
                    newBitmap,
                    renderItem.Width,
                    renderItem.Height,
                    renderItem.TextLayout
                );

                // 绘制
                DrawBitmapAtPosition(newBitmap, renderItem, x, y);
            }
            else
            {
                // 创建失败，忽略
#if DEBUG
                Logger.Log($"CreateTextBitmap failed for '{renderItem.Item.Text}', ignore it");
#endif
                // DrawDanmakuTextDirect(renderItem, x, y);
            }
        }
    }

    /// <summary>
    /// 在指定位置绘制位图（提取的公共方法）.
    /// </summary>
    private void DrawBitmapAtPosition(
        ID2D1Bitmap1 bitmap,
        DanmakuDirectXRenderItem renderItem,
        float x,
        float y
    )
    {
        if (renderItem.Item is null)
        {
            return;
        }

        // 保存当前变换矩阵
        Matrix3x2 prevTransform = _resourceManager.D2DContext.Transform;

        // 计算绘制位置（需要减去描边偏移）
        var offset = renderItem.Item.HasOutline ? (float)_configState.OutlineSize : 0;

        // 设置到目标位置（🚀 优化：重用 Matrix3x2）
        _reusableMatrix.M11 = 1;
        _reusableMatrix.M12 = 0;
        _reusableMatrix.M21 = 0;
        _reusableMatrix.M22 = 1;
        _reusableMatrix.M31 = x - offset;
        _reusableMatrix.M32 = y - offset;
        _resourceManager.D2DContext.Transform = _reusableMatrix * prevTransform;

        // 计算透明度（Advanced 和 Subtitle 模式使用完全不透明）
        float opacity =
            (
                renderItem.Item.Mode == DanmakuMode.Advanced
                || renderItem.Item.Mode == DanmakuMode.Subtitle
            )
                ? 1.0f
                : (float)_configState.TextOpacity;

        // 绘制位图（应用真正的透明度！）
        _resourceManager.D2DContext.DrawBitmap(bitmap, opacity, InterpolationMode.Linear);

        // 恢复变换矩阵
        _resourceManager.D2DContext.Transform = prevTransform;
    }

    #endregion

    #region 其他辅助方法

    /// <summary>
    /// 根据弹幕宽度调整滚动速度.
    /// </summary>
    /// <param name="rollingSpeed">基础滚动速度.</param>
    /// <param name="width">弹幕宽度.</param>
    /// <returns>调整后的滚动速度.</returns>
    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining
    )]
    private static float AdjustRollingSpeedByWidth(float rollingSpeed, float width)
    {
        return rollingSpeed * (Math.Min(width * 0.0015f, 0.2f) + 1f);
    }

    #endregion
}
