using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.View;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频播放页信息区块详情视图模型.
/// </summary>
internal sealed partial class VideoPlayerInfoSectionDetailViewModel(VideoPlayerPageViewModel pageVM)
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    /// <summary>
    /// 视频播放页视图模型.
    /// </summary>
    public VideoPlayerPageViewModel Page { get; } = pageVM;

    /// <inheritdoc/>
    public string Title { get; } = Resources.Information;

    public Task TryFirstLoadAsync() => Task.CompletedTask;
}
