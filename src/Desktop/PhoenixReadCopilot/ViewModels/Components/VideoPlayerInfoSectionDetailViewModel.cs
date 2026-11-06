using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频播放页信息区块详情视图模型.
/// </summary>
public sealed partial class VideoPlayerInfoSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerInfoSectionDetailViewModel"/> class.
    /// </summary>
    public VideoPlayerInfoSectionDetailViewModel(VideoPlayerPageViewModel pageVM) => Page = pageVM;

    /// <summary>
    /// 视频播放页视图模型.
    /// </summary>
    public VideoPlayerPageViewModel Page { get; }

    /// <inheritdoc/>
    public string Title { get; } = Resources.Information;

    [RelayCommand]
    private static Task TryFirstLoadAsync() => Task.CompletedTask;
}
