using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadApi;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频播放器推荐区域详情视图模型.
/// </summary>
internal sealed partial class VideoPlayerRecommendSectionDetailViewModel
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
        Items = [.. items.Select(p => new VideoItemViewModel(p, VideoCardStyle.PlayerRecommend))];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerRecommendSectionDetailViewModel"/> class from dramas.
    /// </summary>
    public VideoPlayerRecommendSectionDetailViewModel(IEnumerable<Drama> dramas)
    {
        Items = [.. dramas.Select(d => new VideoItemViewModel(d, VideoCardStyle.PlayerRecommend))];
    }

    /// <inheritdoc/>
    public string Title { get; } = Resources.Recommend;

    public Task TryFirstLoadAsync() => Task.CompletedTask;
}
