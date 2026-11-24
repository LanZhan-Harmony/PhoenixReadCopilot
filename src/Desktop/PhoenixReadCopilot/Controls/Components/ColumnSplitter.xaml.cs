using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 栏分割器.
/// </summary>
internal sealed partial class ColumnSplitter : UserControl
{
    /// <summary>
    /// 是否进入隐藏状态.
    /// </summary>
    public bool IsHide
    {
        get => (bool)GetValue(IsHideProperty);
        set => SetValue(IsHideProperty, value);
    }

    public static readonly DependencyProperty IsHideProperty = DependencyProperty.Register(
        nameof(IsHide),
        typeof(bool),
        typeof(ColumnSplitter),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 列宽度.
    /// </summary>
    public double ColumnWidth
    {
        get => (double)GetValue(ColumnWidthProperty);
        set => SetValue(ColumnWidthProperty, value);
    }

    public static readonly DependencyProperty ColumnWidthProperty = DependencyProperty.Register(
        nameof(ColumnWidth),
        typeof(double),
        typeof(ColumnSplitter),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 最小列宽度.
    /// </summary>
    public double MinColumnWidth
    {
        get => (double)GetValue(MinColumnWidthProperty);
        set => SetValue(MinColumnWidthProperty, value);
    }

    public static readonly DependencyProperty MinColumnWidthProperty = DependencyProperty.Register(
        nameof(MinColumnWidth),
        typeof(double),
        typeof(ColumnSplitter),
        new PropertyMetadata(220d)
    );

    /// <summary>
    /// 最大列宽度.
    /// </summary>
    public double MaxColumnWidth
    {
        get => (double)GetValue(MaxColumnWidthProperty);
        set => SetValue(MaxColumnWidthProperty, value);
    }

    public static readonly DependencyProperty MaxColumnWidthProperty = DependencyProperty.Register(
        nameof(MaxColumnWidth),
        typeof(double),
        typeof(ColumnSplitter),
        new PropertyMetadata(300d)
    );

    /// <summary>
    /// 是否显示隐藏按钮.
    /// </summary>
    public bool IsHideButtonEnabled
    {
        get => (bool)GetValue(IsHideButtonEnabledProperty);
        set => SetValue(IsHideButtonEnabledProperty, value);
    }

    public static readonly DependencyProperty IsHideButtonEnabledProperty =
        DependencyProperty.Register(
            nameof(IsHideButtonEnabled),
            typeof(bool),
            typeof(ColumnSplitter),
            new PropertyMetadata(true)
        );

    /// <summary>
    /// 是否反转方向. 默认是左向右.
    /// </summary>
    public bool IsInvertDirection
    {
        get => (bool)GetValue(IsInvertDirectionProperty);
        set => SetValue(IsInvertDirectionProperty, value);
    }

    public static readonly DependencyProperty IsInvertDirectionProperty =
        DependencyProperty.Register(
            nameof(IsInvertDirection),
            typeof(bool),
            typeof(ColumnSplitter),
            new PropertyMetadata(null, new PropertyChangedCallback(OnInvertDirectionChanged))
        );

    /// <summary>
    /// 处于显示状态时的提示.
    /// </summary>
    public string ShowTip
    {
        get => (string)GetValue(ShowTipProperty);
        set => SetValue(ShowTipProperty, value);
    }

    public static readonly DependencyProperty ShowTipProperty = DependencyProperty.Register(
        nameof(ShowTip),
        typeof(string),
        typeof(ColumnSplitter),
        new PropertyMetadata("Hide")
    );

    /// <summary>
    /// 处于隐藏状态时的提示.
    /// </summary>
    public string HideTip
    {
        get => (string)GetValue(HideTipProperty);
        set => SetValue(HideTipProperty, value);
    }

    public static readonly DependencyProperty HideTipProperty = DependencyProperty.Register(
        nameof(HideTip),
        typeof(string),
        typeof(ColumnSplitter),
        new PropertyMetadata("Show")
    );

    /// <summary>
    /// 在折叠时不隐藏按钮.
    /// </summary>
    public bool AlwaysShowButtonWhenCollapsed
    {
        get => (bool)GetValue(AlwaysShowButtonWhenCollapsedProperty);
        set => SetValue(AlwaysShowButtonWhenCollapsedProperty, value);
    }

    public static readonly DependencyProperty AlwaysShowButtonWhenCollapsedProperty =
        DependencyProperty.Register(
            nameof(AlwaysShowButtonWhenCollapsed),
            typeof(bool),
            typeof(ColumnSplitter),
            new PropertyMetadata(null)
        );

    public ColumnSplitter()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (IsInvertDirection)
        {
            Sizer.IsDragInverted = true;
            ToggleBtn.Direction = VisibilityToggleButtonDirection.RightToLeftVisible;
            ToggleBtn.Margin = new Thickness(-32, 0, 0, 0);
            ToggleBtn.CornerRadius = new CornerRadius(6, 0, 0, 6);
        }
    }

    private static void OnInvertDirectionChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        ColumnSplitter? instance = d as ColumnSplitter;

        if (instance is not null && instance.IsInvertDirection)
        {
            instance.Sizer.IsDragInverted = true;
            instance.ToggleBtn.Direction = VisibilityToggleButtonDirection.RightToLeftVisible;
            instance.ToggleBtn.Margin = new Thickness(-32, 0, 0, 0);
            instance.ToggleBtn.CornerRadius = new CornerRadius(6, 0, 0, 6);
        }
    }
}
