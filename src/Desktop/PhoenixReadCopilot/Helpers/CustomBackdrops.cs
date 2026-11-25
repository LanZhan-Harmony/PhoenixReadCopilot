using Microsoft.UI;
using Windows.UI;
using Windows.UI.Composition;
using WinUIEx;

namespace PhoenixReadCopilot.Helpers;

/// <summary>
/// 纯色窗口背景材质（支持实时更新颜色）.
/// </summary>
internal sealed partial class SolidColorBackdrop(Color initialColor) : CompositionBrushBackdrop
{
    private CompositionColorBrush? _brush;
    private Color _color = initialColor;

    /// <summary>
    /// 更新纯色背景颜色.
    /// </summary>
    /// <param name="color">目标颜色.</param>
    public void UpdateColor(Color color)
    {
        _color = color;
        _brush?.Color = color;
    }

    protected override CompositionBrush CreateBrush(Compositor compositor)
    {
        _brush = compositor.CreateColorBrush(_color);
        return _brush;
    }
}

/// <summary>
/// 窗口背景模糊材质.
/// </summary>
internal sealed partial class BlurredBackdrop : CompositionBrushBackdrop
{
    protected override CompositionBrush CreateBrush(Compositor compositor) =>
        compositor.CreateHostBackdropBrush();
}

/// <summary>
/// 彩色循环渐变动画窗口背景材质.
/// </summary>
internal sealed partial class ColorAnimatedBackdrop : CompositionBrushBackdrop
{
    protected override CompositionBrush CreateBrush(Compositor compositor)
    {
        CompositionColorBrush brush = compositor.CreateColorBrush(Color.FromArgb(255, 255, 0, 0));
        ColorKeyFrameAnimation animation = compositor.CreateColorKeyFrameAnimation();
        LinearEasingFunction easing = compositor.CreateLinearEasingFunction();
        animation.InsertKeyFrame(0f, Colors.Red, easing);
        animation.InsertKeyFrame(0.333f, Colors.Green, easing);
        animation.InsertKeyFrame(0.667f, Colors.Blue, easing);
        animation.InsertKeyFrame(1f, Colors.Red, easing);
        animation.InterpolationColorSpace = CompositionColorSpace.Hsl;
        animation.Duration = TimeSpan.FromSeconds(15);
        animation.IterationBehavior = AnimationIterationBehavior.Forever;
        brush.StartAnimation("Color", animation);
        return brush;
    }
}
