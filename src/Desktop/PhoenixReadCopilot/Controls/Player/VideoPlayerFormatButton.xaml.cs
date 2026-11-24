using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 视频播放器格式按钮.
/// </summary>
internal sealed partial class VideoPlayerFormatButton : UserControl
{
    private bool _isUpdatingSelection;

    public VideoPlayerPageViewModel? ViewModel
    {
        get => (VideoPlayerPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(VideoPlayerPageViewModel),
        typeof(VideoPlayerFormatButton),
        new PropertyMetadata(null)
    );

    public VideoPlayerFormatButton()
    {
        InitializeComponent();
        Unloaded += (_, _) => OnControlUnloaded();
    }

    private void OnControlUnloaded() => FormatView.ItemsSource = null;

    private void OnFlyoutOpened(object sender, object e)
    {
        if (ViewModel?.Formats is null || ViewModel.SelectedFormat is null)
        {
            return;
        }

        int index = ViewModel.Formats.FindIndex(f =>
            f == ViewModel.SelectedFormat
            || (
                f.Data.Quality == ViewModel.SelectedFormat.Data.Quality
                && f.PlaybackMedia?.Url == ViewModel.SelectedFormat.PlaybackMedia?.Url
            )
        );

        if (index != -1)
        {
            try
            {
                _isUpdatingSelection = true;
                FormatView.Select(index);
            }
            finally
            {
                _isUpdatingSelection = false;
            }
        }
    }

    private void OnFormatSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        if (_isUpdatingSelection || ViewModel is null)
        {
            return;
        }

        if (sender.SelectedItem is PlayerFormatItemViewModel format)
        {
            if (
                format != ViewModel.SelectedFormat
                && (
                    ViewModel.SelectedFormat is null
                    || format.Data.Quality != ViewModel.SelectedFormat.Data.Quality
                    || format.PlaybackMedia?.Url != ViewModel.SelectedFormat.PlaybackMedia?.Url
                )
            )
            {
                _ = ViewModel.ChangeFormatAsync(format);
            }
        }
    }
}
