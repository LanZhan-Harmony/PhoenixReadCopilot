using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Controls.Core;

/// <summary>
/// 播放器.
/// </summary>
internal sealed partial class PlayerPresenter : UserControl
{
    public PlayerViewModelBase? ViewModel
    {
        get => (PlayerViewModelBase?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(PlayerViewModelBase),
        typeof(PlayerPresenter),
        new PropertyMetadata(null)
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerPresenter"/> class.
    /// </summary>
    public PlayerPresenter()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 尝试获取 FFmpeg 播放器.
    /// </summary>
    /// <returns>实例.</returns>
    public FFmpegPlayer? TryGetFFmpegPlayer() => this.FindDescendant<FFmpegPlayer>();
}
