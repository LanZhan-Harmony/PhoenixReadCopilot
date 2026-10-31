using PhoenixReadCopilot.UI.ViewModels.Core;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Core;

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
        _playerElement = new MediaPlayerElement();
        _playerElement.HorizontalAlignment = HorizontalAlignment.Stretch;
        _playerElement.VerticalAlignment = VerticalAlignment.Stretch;
        _playerElement.AreTransportControlsEnabled = false;
        rootGrid.Children.Add(_playerElement);
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
