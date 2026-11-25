using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 短剧播放器选集部分.
/// </summary>
internal sealed partial class VideoPartsSection : UserControl
{
    public VideoPlayerPartSectionDetailViewModel? ViewModel
    {
        get => (VideoPlayerPartSectionDetailViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(VideoPlayerPartSectionDetailViewModel),
        typeof(VideoPartsSection),
        new PropertyMetadata(null, (d, e) =>
            ((VideoPartsSection)d).OnViewModelChanged(
                (VideoPlayerPartSectionDetailViewModel?)e.OldValue,
                (VideoPlayerPartSectionDetailViewModel?)e.NewValue
            )
        )
    );

    public VideoPartsSection()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        UpdateSelection();
        ScrollToCurrentAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel?.PartChanged -= OnPartChanged;
    }

    private void OnViewModelChanged(
        VideoPlayerPartSectionDetailViewModel? oldValue,
        VideoPlayerPartSectionDetailViewModel? newValue
    )
    {
        oldValue?.PartChanged -= OnPartChanged;

        if (newValue is null)
        {
            return;
        }

        newValue.PartChanged += OnPartChanged;
        UpdateSelection();
        ScrollToCurrentAsync();
    }

    private void OnPartSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        if (
            IsLoaded
            && ViewModel is not null
            && sender.SelectedItem is Episode episode
            && episode != ViewModel.SelectedPart
        )
        {
            ViewModel.SelectPart(episode);
        }
    }

    private void OnPartChanged(object? sender, EventArgs e) => UpdateSelection();

    private void UpdateSelection()
    {
        if (ViewModel is { SelectedPart: Episode episode, Parts: not null })
        {
            int index = ViewModel.Parts.ToList().IndexOf(episode);
            if (index >= 0)
            {
                View.Select(index);
            }
        }
    }

    private async void ScrollToCurrentAsync()
    {
        if (ViewModel is { SelectedPart: Episode episode, Parts: not null })
        {
            int index = ViewModel.Parts.ToList().IndexOf(episode);
            if (index >= 0)
            {
                await Task.Delay(100);
                View.StartBringItemIntoView(
                    index,
                    new BringIntoViewOptions { VerticalAlignmentRatio = 0.5 }
                );
            }
        }
    }
}
