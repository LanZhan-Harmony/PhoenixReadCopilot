using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频统计区域.
/// </summary>
internal sealed partial class VideoStatControl : UserControl
{
    public VideoPlayerPageViewModel? ViewModel
    {
        get => (VideoPlayerPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(VideoPlayerPageViewModel),
        typeof(VideoStatControl),
        new PropertyMetadata(null)
    );

    public VideoStatControl()
    {
        InitializeComponent();
    }
}
