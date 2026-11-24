using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Rank;

/// <summary>
/// 榜单页面主体容器.
/// </summary>
internal sealed partial class RankMainBody : UserControl
{
    public RankPageViewModel? ViewModel
    {
        get => (RankPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(RankPageViewModel),
        typeof(RankMainBody),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="RankMainBody"/> class.
    /// </summary>
    public RankMainBody()
    {
        InitializeComponent();
    }
}
