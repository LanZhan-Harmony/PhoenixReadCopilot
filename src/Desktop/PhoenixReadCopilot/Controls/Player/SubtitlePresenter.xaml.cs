using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 字幕显示器.
/// </summary>
internal sealed partial class SubtitlePresenter : UserControl
{
    public SubtitleViewModel? ViewModel
    {
        get => (SubtitleViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(SubtitleViewModel),
        typeof(SubtitlePresenter),
        new PropertyMetadata(null)
    );

    public SubtitlePresenter()
    {
        InitializeComponent();
    }
}
