using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频播放器推荐区域详情视图模型.
/// </summary>
public sealed partial class VideoPlayerRecommendSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    [ObservableProperty]
    public partial List<VideoItemViewModel> Items { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerRecommendSectionDetailViewModel"/> class.
    /// </summary>
    public VideoPlayerRecommendSectionDetailViewModel(IList<VideoInformation> items)
    {
        Items = items
            .Select(p => new VideoItemViewModel(p, VideoCardStyle.PlayerRecommend))
            .ToList();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerRecommendSectionDetailViewModel"/> class from dramas.
    /// </summary>
    public VideoPlayerRecommendSectionDetailViewModel(IEnumerable<Drama> dramas)
    {
        Items = dramas
            .Select(d => new VideoItemViewModel(d, VideoCardStyle.PlayerRecommend))
            .ToList();
    }

    /// <inheritdoc/>
    public string Title { get; } = Resources.Recommend;

    [RelayCommand]
    private static Task TryFirstLoadAsync() => Task.CompletedTask;
}
