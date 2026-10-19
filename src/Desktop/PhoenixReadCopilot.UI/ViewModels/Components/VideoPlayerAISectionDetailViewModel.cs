using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 视频播放页 AI 区块详情视图模型.
/// </summary>
public sealed partial class VideoPlayerAISectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlayerInfoSectionDetailViewModel"/> class.
    /// </summary>
    public VideoPlayerAISectionDetailViewModel(AIViewModel aiVM) => AI = aiVM;

    /// <summary>
    /// 视频播放页视图模型.
    /// </summary>
    public AIViewModel AI { get; }

    /// <inheritdoc/>
    public string Title { get; } = ResourceToolkit.GetLocalizedString(StringNames.AIService);

    [RelayCommand]
    private Task TryFirstLoadAsync()
    {
        AI?.InitializeCommand.Execute(default);
        return Task.CompletedTask;
    }
}
