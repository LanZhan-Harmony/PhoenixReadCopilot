using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

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

    /// <inheritdoc/>
    public string Title { get; } = Resources.Recommend;

    [RelayCommand]
    private static Task TryFirstLoadAsync() => Task.CompletedTask;
}
