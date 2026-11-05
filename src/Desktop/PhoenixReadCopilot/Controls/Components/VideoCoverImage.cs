using System.Collections.Concurrent;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 视频封面图片.
/// </summary>
public sealed partial class VideoCoverImage : ImageExBase
{
    private static readonly ConcurrentDictionary<Uri, double> _aspectRatioCache = new();

    /// <summary>
    /// <see cref="AspectRatio"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty AspectRatioProperty = DependencyProperty.Register(
        nameof(AspectRatio),
        typeof(double),
        typeof(VideoCoverImage),
        new PropertyMetadata(16.0 / 9.0, OnAspectRatioPropertyChanged)
    );

    /// <summary>
    /// <see cref="AutoAdjustAspectRatio"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty AutoAdjustAspectRatioProperty =
        DependencyProperty.Register(
            nameof(AutoAdjustAspectRatio),
            typeof(bool),
            typeof(VideoCoverImage),
            new PropertyMetadata(true)
        );

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoCoverImage"/> class.
    /// </summary>
    public VideoCoverImage()
    {
        DecodeWidth = 336;
        DecodeHeight = 189;
        RegisterPropertyChangedCallback(SourceProperty, OnSourcePropertyChanged);
        RegisterPropertyChangedCallback(DecodeWidthProperty, OnDecodeWidthPropertyChanged);
    }

    /// <summary>
    /// 宽高比 (宽 / 高).
    /// </summary>
    public double AspectRatio
    {
        get => (double)GetValue(AspectRatioProperty);
        set => SetValue(AspectRatioProperty, value);
    }

    /// <summary>
    /// 是否在图片加载后自动根据实际图片宽高调整 AspectRatio.
    /// </summary>
    public bool AutoAdjustAspectRatio
    {
        get => (bool)GetValue(AutoAdjustAspectRatioProperty);
        set => SetValue(AutoAdjustAspectRatioProperty, value);
    }

    private static void OnAspectRatioPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is VideoCoverImage control)
        {
            var ratio = (double)e.NewValue;
            if (ratio > 0)
            {
                control.DecodeHeight = Math.Max(1, Math.Round(control.DecodeWidth / ratio));
                control.InvalidateMeasure();
            }
        }
    }

    private static void OnDecodeWidthPropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (sender is VideoCoverImage control && control.AspectRatio > 0)
        {
            control.DecodeHeight = Math.Max(
                1,
                Math.Round(control.DecodeWidth / control.AspectRatio)
            );
        }
    }

    private void OnSourcePropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (!AutoAdjustAspectRatio)
        {
            return;
        }

        if (Source != null && _aspectRatioCache.TryGetValue(Source, out var cachedRatio))
        {
            if (Math.Abs(AspectRatio - cachedRatio) > 0.01)
            {
                AspectRatio = cachedRatio;
            }
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // 如果显式设置了 Width 和 Height，直接使用显式尺寸
        if (!double.IsNaN(Width) && !double.IsNaN(Height))
        {
            return new Size(Width, Height);
        }

        var ratio = AspectRatio > 0 ? AspectRatio : (16.0 / 9.0);

        // 仅显式设置了 Width
        if (!double.IsNaN(Width))
        {
            return new Size(Width, Width / ratio);
        }

        // 仅显式设置了 Height
        if (!double.IsNaN(Height))
        {
            return new Size(Height * ratio, Height);
        }

        // 高度无限制（例如 StaggeredLayout 或 RowDefinition Height="Auto"），极速返回尺寸，避开模板遍历
        if (double.IsPositiveInfinity(availableSize.Height))
        {
            var targetWidth =
                double.IsPositiveInfinity(availableSize.Width) || availableSize.Width <= 0
                    ? DecodeWidth
                    : availableSize.Width;
            var targetHeight = targetWidth / ratio;
            return new Size(targetWidth, targetHeight);
        }

        // 宽度无限制（例如水平滚动列表），根据高度自适应宽度
        if (double.IsPositiveInfinity(availableSize.Width))
        {
            var targetHeight = availableSize.Height <= 0 ? DecodeHeight : availableSize.Height;
            var targetWidth = targetHeight * ratio;
            return new Size(targetWidth, targetHeight);
        }

        // 两个方向都有约束时才走基类测量
        var baseSize = base.MeasureOverride(availableSize);
        if (baseSize.Width <= 0 || baseSize.Height <= 0)
        {
            return availableSize;
        }

        return baseSize;
    }

    /// <inheritdoc/>
    protected override void DrawImage(CanvasBitmap canvasBitmap)
    {
        var bitmapWidth = canvasBitmap.SizeInPixels.Width;
        var bitmapHeight = canvasBitmap.SizeInPixels.Height;

        if (bitmapWidth > 0 && bitmapHeight > 0)
        {
            var realRatio = (double)bitmapWidth / bitmapHeight;
            // 限制极端比例，避免瀑布流中过长或过扁的异常卡片影响体验 (0.5 ~ 2.0，涵盖 9:16 到 2:1)
            realRatio = Math.Clamp(realRatio, 0.5, 2.0);

            if (Source != null)
            {
                if (_aspectRatioCache.Count > 1000)
                {
                    _aspectRatioCache.Clear();
                }

                _aspectRatioCache[Source] = realRatio;
            }

            if (AutoAdjustAspectRatio && Math.Abs(AspectRatio - realRatio) > 0.01)
            {
                AspectRatio = realRatio;
            }
        }

        var targetWidth = (float)DecodeWidth;
        var targetHeight = (float)DecodeHeight;

        // 确保 CanvasImageSource 具有与当前 DecodeWidth / DecodeHeight 匹配的分辨率
        if (
            CanvasImageSource == null
            || Math.Abs(CanvasImageSource.Size.Width - targetWidth) > 0.5
            || Math.Abs(CanvasImageSource.Size.Height - targetHeight) > 0.5
        )
        {
            var device = CanvasDevice.GetSharedDevice();
            CanvasImageSource = new CanvasImageSource(
                device,
                targetWidth,
                targetHeight,
                96,
                CanvasAlphaMode.Ignore
            );
        }

        var sourceRect = new Rect(0, 0, bitmapWidth, bitmapHeight);
        var destRect = new Rect(0, 0, targetWidth, targetHeight);
        var cropRect = GetCenterCropRect(destRect, sourceRect);

        using var drawingSession = CanvasImageSource.CreateDrawingSession(ClearColor);
        drawingSession.DrawImage(canvasBitmap, destRect, cropRect);
    }
}
