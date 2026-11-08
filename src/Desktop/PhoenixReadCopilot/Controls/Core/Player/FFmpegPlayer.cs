using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Core;

/// <summary>
/// FFmpeg 播放器.
/// </summary>
public sealed partial class FFmpegPlayer : LayoutControlBase<FFmpegPlayerViewModel>
{
    private MediaPlayerElement _playerElement;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFmpegPlayer"/> class.
    /// </summary>
    public FFmpegPlayer() => DefaultStyleKey = typeof(FFmpegPlayer);

    /// <inheritdoc/>
    protected override async void OnControlLoaded()
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.InitializeAsync(_playerElement);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        var rootGrid = GetTemplateChild("RootGrid") as Grid;
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
    }

    /// <inheritdoc/>
    protected override async void OnViewModelChanged(
        FFmpegPlayerViewModel? oldValue,
        FFmpegPlayerViewModel? newValue
    )
    {
        if (_playerElement is null)
        {
            return;
        }

        await newValue?.InitializeAsync(_playerElement);
    }
}
