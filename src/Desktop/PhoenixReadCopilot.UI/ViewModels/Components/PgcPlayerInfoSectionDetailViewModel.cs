using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;
using PhoenixReadCopilot.UI.ViewModels.Items;
using PhoenixReadCopilot.UI.ViewModels.View;
using Richasy.WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// PGC播放页信息区块详情视图模型.
/// </summary>
public sealed partial class PgcPlayerInfoSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PgcPlayerInfoSectionDetailViewModel"/> class.
    /// </summary>
    public PgcPlayerInfoSectionDetailViewModel(PgcPlayerPageViewModel pageVM) => Page = pageVM;

    /// <summary>
    /// 视频播放页视图模型.
    /// </summary>
    public PgcPlayerPageViewModel Page { get; }

    /// <inheritdoc/>
    public string Title { get; } = ResourceToolkit.GetLocalizedString(StringNames.Information);

    [RelayCommand]
    private static Task TryFirstLoadAsync() => Task.CompletedTask;
}
