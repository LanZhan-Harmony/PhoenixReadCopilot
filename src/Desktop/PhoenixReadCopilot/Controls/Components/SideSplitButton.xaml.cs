using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 侧边分割按钮.
/// </summary>
internal sealed partial class SideSplitButton : UserControl
{
    /// <summary>
    /// 是否进入隐藏状态.
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
        typeof(SideSplitButton),
        new PropertyMetadata(null, new PropertyChangedCallback(OnIsHideChanged))
    );

    /// <summary>
    /// 是否反转方向. 默认是左向右.
    /// </summary>
    public bool IsInvertDirection
    {
        get => (bool)GetValue(IsInvertDirectionProperty);
        set => SetValue(IsInvertDirectionProperty, value);
    }

    /// <summary>
    /// <see cref="IsInvertDirection"/> 依赖属性.
    /// </summary>
    public static readonly DependencyProperty IsInvertDirectionProperty =
        DependencyProperty.Register(
            nameof(IsInvertDirection),
            typeof(bool),
            typeof(SideSplitButton),
            new PropertyMetadata(null)
        );

    public SideSplitButton()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SymbolControl.Symbol = GetSymbol();
    }

    private static void OnIsHideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        SideSplitButton? instance = d as SideSplitButton;
        instance?.SymbolControl.Symbol = instance.GetSymbol();
    }

    private FluentIcons.Common.Symbol GetSymbol()
    {
        return IsHide
            ? IsInvertDirection
                ? FluentIcons.Common.Symbol.PanelRightExpand
                : FluentIcons.Common.Symbol.PanelLeftExpand
            : IsInvertDirection
                ? FluentIcons.Common.Symbol.PanelRightContract
                : FluentIcons.Common.Symbol.PanelLeftContract;
    }

    private void OnBtnClick(object sender, RoutedEventArgs e) => IsHide = !IsHide;
}
