using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频播放器左侧面板.
/// </summary>
internal sealed partial class VideoPlayerTransportLeftPanel : UserControl
{
    public VideoPlayerPageViewModel? ViewModel
    {
        get => (VideoPlayerPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(VideoPlayerPageViewModel),
        typeof(VideoPlayerTransportLeftPanel),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerTransportLeftPanel"/> class.
    /// </summary>
    public VideoPlayerTransportLeftPanel()
    {
        InitializeComponent();
    }
}
