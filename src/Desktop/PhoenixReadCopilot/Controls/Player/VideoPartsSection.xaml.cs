using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Components;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.Controls.Player;

/// <summary>
/// 短剧播放器选集部分.
/// </summary>
public sealed partial class VideoPartsSection : VideoPartsSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPartsSection"/> class.
    /// </summary>
    public VideoPartsSection() => InitializeComponent();

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        if (ViewModel is null)
        {
            return;
        }

        UpdateSelection();
        ScrollToCurrentAsync();
    }

    /// <inheritdoc/>
    protected override void OnControlUnloaded()
    {
        View.ItemsSource = default;
        if (ViewModel is not null)
        {
            ViewModel.PartChanged -= OnPartChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnViewModelChanged(
        VideoPlayerPartSectionDetailViewModel? oldValue,
        VideoPlayerPartSectionDetailViewModel? newValue
    )
    {
        if (oldValue is not null)
        {
            oldValue.PartChanged -= OnPartChanged;
        }

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
        if (IsLoaded && sender.SelectedItem is Episode episode && episode != ViewModel.SelectedPart)
        {
            ViewModel.SelectPartCommand.Execute(episode);
        }
    }

    private void OnPartChanged(object? sender, EventArgs e) => UpdateSelection();

    private void UpdateSelection()
    {
        if (ViewModel?.SelectedPart is Episode episode && ViewModel.Parts is not null)
        {
            var index = ViewModel.Parts.ToList().IndexOf(episode);
            if (index >= 0)
            {
                View.Select(index);
            }
        }
    }

    private async void ScrollToCurrentAsync()
    {
        if (ViewModel?.SelectedPart is Episode episode && ViewModel.Parts is not null)
        {
            var index = ViewModel.Parts.ToList().IndexOf(episode);
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

/// <summary>
/// 视频播放器分P部分基类.
/// </summary>
public abstract class VideoPartsSectionBase
    : LayoutUserControlBase<VideoPlayerPartSectionDetailViewModel> { }
