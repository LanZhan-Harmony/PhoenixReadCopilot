using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Filter;

/// <summary>
/// 娱乐索引侧边栏控件.
/// </summary>
internal sealed partial class FilterIndexSideControl : UserControl
{
    public FilterPageViewModel? ViewModel
    {
        get => (FilterPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(FilterPageViewModel),
        typeof(FilterIndexSideControl),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterIndexSideControl"/> class.
    /// </summary>
    public FilterIndexSideControl()
    {
        InitializeComponent();
        Unloaded += (_, _) => FilterRepeater.ItemsSource = null;
    }
}
