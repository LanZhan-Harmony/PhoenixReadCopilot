using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.ViewModels.Items;
using PhoenixReadCopilot.UI.ViewModels.View;
using Richasy.BiliKernel.Models.Media;
using Richasy.WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 视频播放器合集视图模型.
/// </summary>
public sealed partial class VideoPlayerSeasonSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    /// <summary>
    /// 当前播放页面正在播放的视频Id.
    /// </summary>
    private readonly string _videoId;

    [ObservableProperty]
    public partial List<VideoSeason> Seasons { get; set; }

    [ObservableProperty]
    public partial List<VideoItemViewModel> Items { get; set; }

    [ObservableProperty]
    public partial VideoSeason SelectedSeason { get; set; }

    [ObservableProperty]
    public partial VideoItemViewModel SelectedItem { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerSeasonSectionDetailViewModel"/> class.
    /// </summary>
    public VideoPlayerSeasonSectionDetailViewModel(
        VideoPlayerPageViewModel page,
        IList<VideoSeason> seasons,
        string videoId
    )
    {
        Page = page;
        Seasons = seasons.ToList();
        _videoId = videoId;
        var selectedSeason = Seasons.FirstOrDefault(p =>
            p.Videos.Any(q => q.Identifier.Id == _videoId)
        );
        ChangeSeason(selectedSeason);
    }

    /// <inheritdoc/>
    public string Title => Resources.UgcSeason;

    /// <summary>
    /// 页面视图模型.
    /// </summary>
    public VideoPlayerPageViewModel Page { get; }

    [RelayCommand]
    private static Task TryFirstLoadAsync() => Task.CompletedTask;

    [RelayCommand]
    private void ChangeSeason(VideoSeason season)
    {
        if (season is null || season == SelectedSeason)
        {
            return;
        }

        SelectedSeason = season;
        Items = season
            .Videos.Select(p => new VideoItemViewModel(
                p,
                Models.Constants.VideoCardStyle.PlayerSeason
            ))
            .ToList();
        SelectedItem = Items.FirstOrDefault(p => p.Data.Identifier.Id == _videoId);
    }
}
