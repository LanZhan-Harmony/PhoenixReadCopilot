using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadApi;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频播放器推荐区域详情视图模型.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VideoPlayerRecommendSectionDetailViewModel"/> class from dramas.
/// </remarks>
internal sealed partial class VideoPlayerRecommendSectionDetailViewModel(IEnumerable<Drama> dramas)
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    [ObservableProperty]
    public partial List<DramaItemViewModel> Items { get; set; } =
    [.. dramas.Select(d => new DramaItemViewModel(d, VideoCardStyle.PlayerRecommend))];

    /// <inheritdoc/>
    public string Title { get; } = Resources.Recommend;

    public Task TryFirstLoadAsync() => Task.CompletedTask;
}
