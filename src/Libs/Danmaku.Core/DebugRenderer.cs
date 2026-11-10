using Vortice;
using Vortice.Direct2D1;
using Vortice.DirectWrite;

namespace Danmaku.Core;

/// <summary>
/// 简化的调试信息渲染器
/// </summary>
public sealed class DebugRenderer : IDisposable
{
    private const float PanelPadding = 10f;
    private const float LineSpacing = 20f;
    private const float DebugFontSize = 12f;
    private const float CornerRadius = 5f;
    private const byte BackgroundOpacity = 180;

    private readonly ID2D1DeviceContext _d2dContext;
    private readonly IDWriteFactory _dwriteFactory;

    // 缓存资源，避免每帧重新创建
    private IDWriteTextFormat? _cachedTextFormat;
    private ID2D1SolidColorBrush? _cachedBackgroundBrush;
    private ID2D1SolidColorBrush? _cachedTextBrush;
    private float _lastScale = -1f;

    public DebugRenderer(ID2D1DeviceContext d2dContext, IDWriteFactory dWriteFactory)
    {
        _d2dContext = d2dContext ?? throw new ArgumentNullException(nameof(d2dContext));
        _dwriteFactory = dWriteFactory ?? throw new ArgumentNullException(nameof(dWriteFactory));
    }

    /// <summary>
    /// 渲染调试信息
    /// </summary>
    public void RenderDebugInfo(DebugInfo debugInfo, double scale)
    {
        if (_d2dContext == null)
        {
            return;
        }

        try
        {
            var lines = new[]
            {
                "弹幕调试信息:",
                $"缓冲区大小: {debugInfo.BufferWidth}x{debugInfo.BufferHeight}",
                $"逻辑大小: {debugInfo.LogicalWidth:F1}x{debugInfo.LogicalHeight:F1}",
                $"缩放比例: {debugInfo.ScaleX:F2}x{debugInfo.ScaleY:F2}",
                $"弹幕数量: {debugInfo.DanmakuCount}",
                $"运行时间: {debugInfo.RunningTime:mm\\:ss}",
                $"帧率: {debugInfo.FrameRate:F1} FPS",
            };

            DrawDebugPanel(lines, scale);
        }
        catch
        {
            // 忽略调试渲染错误，避免影响正常渲染
        }
    }

    /// <summary>
    /// 绘制调试面板
    /// </summary>
    private void DrawDebugPanel(string[] lines, double scale)
    {
        var floatScale = (float)scale;

        // 如果缩放变化，重新创建资源
        if (Math.Abs(_lastScale - floatScale) > 0.01f)
        {
            DisposeResources();
            CreateResources(floatScale);
            _lastScale = floatScale;
        }

        // 如果资源创建失败，直接返回
        if (_cachedTextFormat == null || _cachedBackgroundBrush == null || _cachedTextBrush == null)
        {
            return;
        }

        // 应用缩放因子到面板大小和位置
        var scaledPadding = PanelPadding * floatScale;
        var scaledLineSpacing = LineSpacing * floatScale;
        var scaledCornerRadius = CornerRadius * floatScale;

        // 计算面板大小（应用缩放）
        var panelWidth = 250f * floatScale;
        var panelHeight = lines.Length * scaledLineSpacing + scaledPadding * 2;

        // 绘制背景（应用缩放）
        RoundedRectangle backgroundRect = new()
        {
            Rect = new Vortice.RawRectF(
                scaledPadding,
                scaledPadding,
                scaledPadding + panelWidth,
                scaledPadding + panelHeight
            ),
            RadiusX = scaledCornerRadius,
            RadiusY = scaledCornerRadius,
        };

        _d2dContext.FillRoundedRectangle(backgroundRect, _cachedBackgroundBrush);

        // 绘制文本（应用缩放）
        var textY = scaledPadding + scaledPadding / 2;
        foreach (var line in lines)
        {
            RawRectF textRect = new(
                scaledPadding + (scaledPadding / 2),
                textY,
                scaledPadding + panelWidth - (scaledPadding / 2),
                textY + scaledLineSpacing
            );

            _d2dContext.DrawText(line, _cachedTextFormat, textRect, _cachedTextBrush);
            textY += scaledLineSpacing;
        }
    }

    /// <summary>
    /// 创建缓存的资源
    /// </summary>
    private void CreateResources(float scale)
    {
        try
        {
            var scaledFontSize = DebugFontSize * scale;

            // 创建文本格式
            _cachedTextFormat = _dwriteFactory.CreateTextFormat(
                "Consolas",
                FontWeight.Bold,
                FontStyle.Normal,
                FontStretch.Normal,
                scaledFontSize
            );

            // 创建背景刷子
            _cachedBackgroundBrush = _d2dContext.CreateSolidColorBrush(
                new Vortice.Mathematics.Color((byte)0, (byte)0, (byte)0, BackgroundOpacity)
            );

            // 创建文本刷子
            _cachedTextBrush = _d2dContext.CreateSolidColorBrush(
                new Vortice.Mathematics.Color((byte)255, (byte)255, (byte)255, (byte)255)
            );
        }
        catch
        {
            // 如果创建失败，清理部分创建的资源
            DisposeResources();
        }
    }

    /// <summary>
    /// 释放缓存的资源
    /// </summary>
    private void DisposeResources()
    {
        _cachedTextFormat?.Dispose();
        _cachedTextFormat = null;

        _cachedBackgroundBrush?.Dispose();
        _cachedBackgroundBrush = null;

        _cachedTextBrush?.Dispose();
        _cachedTextBrush = null;
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void Dispose()
    {
        DisposeResources();
        GC.SuppressFinalize(this);
    }
}
