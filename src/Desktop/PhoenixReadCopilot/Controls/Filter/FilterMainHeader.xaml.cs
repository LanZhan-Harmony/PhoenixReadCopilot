using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Filter;

/// <summary>
/// 影院主页顶部.
/// </summary>
internal sealed partial class FilterMainHeader : UserControl
{
    public FilterPageViewModel? ViewModel
    {
        get => (FilterPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(FilterPageViewModel),
        typeof(FilterMainHeader),
        new PropertyMetadata(null)
    );

    public FilterMainHeader()
    {
        InitializeComponent();
    }
}
