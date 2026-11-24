using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 骨架占位图.
/// </summary>
internal sealed partial class ShimmerLayout : Control
{
    /// <summary>
    /// 是否激活.
    /// </summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(ShimmerLayout),
        new PropertyMetadata(null)
    );

    public ShimmerLayout()
    {
        DefaultStyleKey = typeof(ShimmerLayout);
    }
}
