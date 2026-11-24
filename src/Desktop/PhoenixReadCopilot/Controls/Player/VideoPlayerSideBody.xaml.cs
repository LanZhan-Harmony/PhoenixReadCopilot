using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频播放页侧边栏.
/// </summary>
internal sealed partial class VideoPlayerSideBody : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(VideoPlayerPageViewModel),
        typeof(VideoPlayerSideBody),
        new PropertyMetadata(null)
    );

    public VideoPlayerPageViewModel? ViewModel
    {
        get => (VideoPlayerPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerSideBody"/> class.
    /// </summary>
    public VideoPlayerSideBody()
    {
        InitializeComponent();
    }
}
