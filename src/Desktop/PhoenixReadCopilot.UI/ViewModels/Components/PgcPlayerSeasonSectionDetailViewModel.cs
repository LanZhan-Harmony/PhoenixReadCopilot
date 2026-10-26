using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Models.Media;
using Richasy.WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// PGC 播放器页面剧集视图模型.
/// </summary>
public sealed partial class PgcPlayerSeasonSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    [ObservableProperty] private List<SeasonItemViewModel>? _items;

    [ObservableProperty] private SeasonItemViewModel? _selectedItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="PgcPlayerSeasonSectionDetailViewModel"/> class.
    /// </summary>
    public PgcPlayerSeasonSectionDetailViewModel(IList<SeasonInformation> items, string seasonId)
    {
        Items = items.Select(p => new SeasonItemViewModel(p, SeasonCardStyle.Player)).ToList();
        SelectedItem = Items.FirstOrDefault(p => p.Data.Identifier.Id == seasonId);
    }

    /// <inheritdoc/>
    public string Title { get; } = Resources.Seasons;

    [RelayCommand]
    private static Task TryFirstLoadAsync() => Task.CompletedTask;
}
