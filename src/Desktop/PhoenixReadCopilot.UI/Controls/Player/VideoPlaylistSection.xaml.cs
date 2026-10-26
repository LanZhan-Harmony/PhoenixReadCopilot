using PhoenixReadCopilot.UI.Models;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.UI.Controls.Player;

/// <summary>
/// 视频播放列表.
/// </summary>
public sealed partial class VideoPlaylistSection : VideoPlaylistSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlaylistSection"/> class.
    /// </summary>
    public VideoPlaylistSection() => InitializeComponent();

    /// <inheritdoc/>
    protected override async void OnControlLoaded() => await CheckSelectedItemAsync();

    protected override void OnControlUnloaded() => View.ItemsSource = default;

    /// <inheritdoc/>
    protected override async void OnViewModelChanged(
        VideoPlayerPlaylistSectionDetailViewModel? oldValue,
        VideoPlayerPlaylistSectionDetailViewModel? newValue
    ) => await CheckSelectedItemAsync();

    private void OnVideoSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        var item = sender.SelectedItem as VideoItemViewModel;
        if (item is not null && ViewModel.SelectedItem != item)
        {
            ViewModel.SelectedItem = item;
            ViewModel.Page.InitializePageCommand.Execute(new VideoSnapshot(item.Data));
        }
    }

    private async Task CheckSelectedItemAsync()
    {
        await Task.Delay(100);
        if (ViewModel.SelectedItem is null)
        {
            return;
        }

        var index = ViewModel.Items.ToList().IndexOf(ViewModel.SelectedItem);
        View.Select(index);
        View.StartBringItemIntoView(
            index,
            new BringIntoViewOptions { VerticalAlignmentRatio = 0.5 }
        );
    }
}

/// <summary>
/// 视频播放列表基类.
/// </summary>
public abstract class VideoPlaylistSectionBase
    : LayoutUserControlBase<VideoPlayerPlaylistSectionDetailViewModel> { }
