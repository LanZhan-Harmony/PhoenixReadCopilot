using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Search;

/// <summary>
/// 搜索页头部.
/// </summary>
internal sealed partial class SearchHeader : UserControl
{
    public SearchPageViewModel? ViewModel
    {
        get => (SearchPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(SearchPageViewModel),
        typeof(SearchHeader),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchHeader"/> class.
    /// </summary>
    public SearchHeader()
    {
        InitializeComponent();
    }
}
