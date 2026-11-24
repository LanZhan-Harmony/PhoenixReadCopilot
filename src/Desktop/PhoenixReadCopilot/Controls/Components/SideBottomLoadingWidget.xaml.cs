using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 侧边底部加载控件.
/// </summary>
internal sealed partial class SideBottomLoadingWidget : UserControl
{
    /// <summary>
    /// 文本.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(SideBottomLoadingWidget),
        new PropertyMetadata(null)
    );

    public SideBottomLoadingWidget()
    {
        InitializeComponent();
    }
}
