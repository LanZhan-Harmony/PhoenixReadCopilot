using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Filter;

/// <summary>
/// 影院侧边栏头部.
/// </summary>
internal sealed partial class FilterSideHeader : UserControl
{
    public FilterPageViewModel? ViewModel
    {
        get => (FilterPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(FilterPageViewModel),
        typeof(FilterSideHeader),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterSideHeader"/> class.
    /// </summary>
    public FilterSideHeader()
    {
        InitializeComponent();
    }
}
