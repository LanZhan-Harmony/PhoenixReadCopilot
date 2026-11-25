using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 可见性切换按钮.
/// </summary>
internal sealed partial class VisibilityToggleButton : UserControl
{
    /// <summary>
    /// 获取或设置方向.
    /// </summary>
    public VisibilityToggleButtonDirection Direction
    {
        get => (VisibilityToggleButtonDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    /// <summary>
    /// <see cref="Direction"/> 依赖属性.
    /// </summary>
    public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(
        nameof(Direction),
        typeof(VisibilityToggleButtonDirection),
        typeof(VisibilityToggleButton),
        new PropertyMetadata(
            VisibilityToggleButtonDirection.LeftToRightVisible,
            new PropertyChangedCallback(OnDirectionChanged)
        )
    );

    /// <summary>
    /// 是否已经隐藏指定目标.
    /// </summary>
    public bool IsHide
    {
        get => (bool)GetValue(IsHideProperty);
        set => SetValue(IsHideProperty, value);
    }

    /// <summary>
    /// <see cref="IsHide"/> 依赖属性.
    /// </summary>
    public static readonly DependencyProperty IsHideProperty = DependencyProperty.Register(
        nameof(IsHide),
        typeof(bool),
        typeof(VisibilityToggleButton),
        new PropertyMetadata(null, new PropertyChangedCallback(OnIsHideChanged))
    );

    /// <summary>
    /// 在折叠时不隐藏按钮.
    /// </summary>
    public bool AlwaysShowWhenCollapsed
    {
        get => (bool)GetValue(AlwaysShowWhenCollapsedProperty);
        set => SetValue(AlwaysShowWhenCollapsedProperty, value);
    }

    /// <summary>
    /// <see cref="AlwaysShowWhenCollapsed"/> 依赖属性.
    /// </summary>
    public static readonly DependencyProperty AlwaysShowWhenCollapsedProperty =
        DependencyProperty.Register(
            nameof(AlwaysShowWhenCollapsed),
            typeof(bool),
            typeof(VisibilityToggleButton),
            new PropertyMetadata(null)
        );

    /// <summary>
    /// 处于显示状态时的提示.
    /// </summary>
    public string ShowTip { get; set; } = string.Empty;

    /// <summary>
    /// 处于隐藏状态时的提示.
    /// </summary>
    public string HideTip { get; set; } = string.Empty;

    /// <summary>
    /// 点击事件.
    /// </summary>
    public event EventHandler? Click;

    public VisibilityToggleButton()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CheckButtonStates();
    }

    /// <inheritdoc/>
    protected override void OnPointerEntered(PointerRoutedEventArgs e) => ShowButton();

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerRoutedEventArgs e) => ShowButton();

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerRoutedEventArgs e) => HideButton();

    /// <inheritdoc/>
    protected override void OnPointerCanceled(PointerRoutedEventArgs e) => HideButton();

    private static void OnIsHideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        VisibilityToggleButton? instance = d as VisibilityToggleButton;
        instance?.CheckButtonStates();
    }

    private static void OnDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        VisibilityToggleButton? instance = d as VisibilityToggleButton;
        instance?.CheckButtonStates();
    }

    private void CheckButtonStates()
    {
        Icon.Symbol = IsHide
            ? Direction == VisibilityToggleButtonDirection.LeftToRightVisible
                ? Symbol.ChevronRight
                : Symbol.ChevronLeft
            : Direction == VisibilityToggleButtonDirection.LeftToRightVisible
                ? Symbol.ChevronLeft
                : Symbol.ChevronRight;
        string tip = IsHide ? HideTip : ShowTip;
        ToolTipService.SetToolTip(Btn, tip);
        AutomationProperties.SetName(Btn, tip);

        FrameworkElement? container = VisualTreeHelper.GetParent(this) as FrameworkElement;
        if (container is not null)
        {
            container.Margin = IsHide
                ? Direction == VisibilityToggleButtonDirection.LeftToRightVisible
                    ? new Thickness(-4, 0, 0, 0)
                    : new Thickness(0, 0, -4, 0)
                : new Thickness(0);

            HideButton();
        }
    }

    private void OnBtnClick(object sender, RoutedEventArgs e)
    {
        IsHide = !IsHide;
        Click?.Invoke(this, EventArgs.Empty);
    }

    private void ShowButton()
    {
        Btn.Visibility = Visibility.Visible;
        BackgroundGrid.Visibility = Visibility.Visible;
    }

    private void HideButton()
    {
        Btn.Visibility = Visibility.Collapsed;
        BackgroundGrid.Visibility = Visibility.Collapsed;
        if (AlwaysShowWhenCollapsed && IsHide && IsEnabled)
        {
            ShowButton();
        }
    }
}

/// <summary>
/// 可见性切换按钮方向.
/// </summary>
public enum VisibilityToggleButtonDirection
{
    /// <summary>
    /// 需要控制的内容在左侧.
    /// </summary>
    LeftToRightVisible,

    /// <summary>
    /// 需要控制的内容在右侧.
    /// </summary>
    RightToLeftVisible,
}
