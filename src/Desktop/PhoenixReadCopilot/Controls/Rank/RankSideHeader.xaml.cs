using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Rank;

/// <summary>
/// 流榜单页面侧边栏头部.
/// </summary>
internal sealed partial class RankSideHeader : UserControl
{
    public RankPageViewModel? ViewModel
    {
        get => (RankPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(RankPageViewModel),
        typeof(RankSideHeader),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="RankSideHeader"/> class.
    /// </summary>
    public RankSideHeader()
    {
        InitializeComponent();
    }
}
