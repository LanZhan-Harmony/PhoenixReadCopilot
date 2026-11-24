using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Controls.Rank;

/// <summary>
/// 榜单区块项控件.
/// </summary>
internal sealed partial class RankSectionItemControl : UserControl
{
    public RankSectionItemViewModel? ViewModel
    {
        get => (RankSectionItemViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(RankSectionItemViewModel),
        typeof(RankSectionItemControl),
        new PropertyMetadata(null)
    );

    public RankSectionItemControl()
    {
        InitializeComponent();
    }
}
