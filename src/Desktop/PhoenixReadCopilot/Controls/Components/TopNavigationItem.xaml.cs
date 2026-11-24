using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 顶部导航栏条目.
/// </summary>
internal sealed partial class TopNavigationItem : UserControl
{
    /// <summary>
    /// 图标.
    /// </summary>
    public Symbol Symbol
    {
        get => (Symbol)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    /// <summary>
    /// 标识 <see cref="Symbol"/> 依赖属性.
    /// </summary>
    public static readonly DependencyProperty SymbolProperty = DependencyProperty.Register(
        nameof(Symbol),
        typeof(Symbol),
        typeof(TopNavigationItem),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// 条目点击时发生.
    /// </summary>
    public event EventHandler Click;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopNavigationItem"/> class.
    /// </summary>
    public TopNavigationItem()
    {
        InitializeComponent();
    }

    private void OnItemClick(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, EventArgs.Empty);
    }
}
