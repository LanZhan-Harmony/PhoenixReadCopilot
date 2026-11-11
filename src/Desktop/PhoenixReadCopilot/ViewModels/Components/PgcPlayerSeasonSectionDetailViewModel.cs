using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// PGC 播放器页面剧集视图模型.
/// </summary>
public sealed partial class PgcPlayerSeasonSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    [ObservableProperty]
    public partial List<SeasonItemViewModel>? Items { get; set; }

    [ObservableProperty]
    public partial SeasonItemViewModel? SelectedItem { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PgcPlayerSeasonSectionDetailViewModel"/> class.
    /// </summary>
    public PgcPlayerSeasonSectionDetailViewModel(IList<SeasonInformation> items, string seasonId)
    {
        Items =
        [
            .. items.Select(p => new SeasonItemViewModel(p, SeasonCardStyle.Player)),
        ];
        SelectedItem = Items.FirstOrDefault(p => p.Data.Identifier.Id == seasonId);
    }

    /// <inheritdoc/>
    public string Title { get; } = Resources.Seasons;

    [RelayCommand]
    private static Task TryFirstLoadAsync() => Task.CompletedTask;
}
