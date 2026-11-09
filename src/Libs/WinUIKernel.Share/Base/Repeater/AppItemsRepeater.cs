using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace WinUIKernel.Share.Base.Repeater;

/// <summary>
/// ItemsRepeater 的封装.
/// </summary>
public partial class AppItemsRepeater : ItemsRepeater
{
    /// <summary>
    /// <see cref="Stretch"/> 依赖属性.
    /// </summary>
    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
        nameof(Stretch),
        typeof(bool),
        typeof(AppItemsRepeater),
        new PropertyMetadata(null)
    );

    private Size _measureSize;

    /// <summary>
    /// 是否拉伸以填满可用空间.
    /// </summary>
    public bool Stretch
    {
        get => (bool)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size returnSize = default;

        try
        {
            returnSize = _measureSize = base.MeasureOverride(availableSize);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"An error occurred when measuring AppItemsRepeater: {e.Message}");
        }

        if (availableSize.Width >= 0 && !double.IsInfinity(availableSize.Width))
        {
            returnSize.Width = Stretch
                ? Math.Max(Math.Max(availableSize.Width - 1, 0), returnSize.Width)
                : Math.Min(Math.Max(availableSize.Width - 1, 0), returnSize.Width);
        }

        if (availableSize.Height >= 0 && !double.IsInfinity(availableSize.Height))
        {
            returnSize.Height = Math.Min(availableSize.Height, returnSize.Height);
        }

        if (Stretch)
        {
            _measureSize = returnSize;
        }

        return returnSize;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        base.ArrangeOverride(_measureSize);
        return finalSize;
    }
}
