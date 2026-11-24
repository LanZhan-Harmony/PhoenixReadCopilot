using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Controls.Core;

/// <summary>
/// FFmpeg 播放器.
/// </summary>
internal sealed partial class FFmpegPlayer : Control
{
    private MediaPlayerElement? _playerElement;

    /// <summary>
    /// 视图模型.
    /// </summary>
    public FFmpegPlayerViewModel? ViewModel
    {
        get => (FFmpegPlayerViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(FFmpegPlayerViewModel),
        typeof(FFmpegPlayer),
        new PropertyMetadata(null, (d, e) =>
            ((FFmpegPlayer)d).OnViewModelChanged(
                (FFmpegPlayerViewModel?)e.OldValue,
                (FFmpegPlayerViewModel?)e.NewValue
            )
        )
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="FFmpegPlayer"/> class.
    /// </summary>
    public FFmpegPlayer()
    {
        DefaultStyleKey = typeof(FFmpegPlayer);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CheckAndInitializePlayer();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Grid? rootGrid = GetTemplateChild("RootGrid") as Grid;
        _playerElement = new MediaPlayerElement
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            AreTransportControlsEnabled = false,
            Background = new SolidColorBrush(Colors.Black),
        };
        if (rootGrid is not null)
        {
            rootGrid.Background = new SolidColorBrush(Colors.Black);
            rootGrid.Children.Add(_playerElement);
        }

        CheckAndInitializePlayer();
    }

    private void OnViewModelChanged(
        FFmpegPlayerViewModel? oldValue,
        FFmpegPlayerViewModel? newValue
    )
    {
        CheckAndInitializePlayer();
    }

    private void CheckAndInitializePlayer()
    {
        if (IsLoaded && ViewModel is not null && _playerElement is not null)
        {
            _ = ViewModel.InitializeAsync(_playerElement);
        }
    }
}
