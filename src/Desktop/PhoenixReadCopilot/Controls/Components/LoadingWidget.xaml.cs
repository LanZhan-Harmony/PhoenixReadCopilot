using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace PhoenixReadCopilot.Controls.Components;

/// <summary>
/// 加载中控件.
/// </summary>
internal sealed partial class LoadingWidget : UserControl
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
        typeof(LoadingWidget),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadingWidget"/> class.
    /// </summary>
    public LoadingWidget()
    {
        InitializeComponent();
    }
}
