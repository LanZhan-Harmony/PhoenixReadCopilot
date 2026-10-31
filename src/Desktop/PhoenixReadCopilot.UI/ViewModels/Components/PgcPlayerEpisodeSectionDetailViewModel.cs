using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.Toolkits;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// PGC 播放器分集详情视图模型.
/// </summary>
public sealed partial class PgcPlayerEpisodeSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    private readonly Action<EpisodeInformation> _episodeSelectedAction;

    [ObservableProperty]
    public partial List<EpisodeItemViewModel>? Episodes { get; set; }

    [ObservableProperty]
    public partial EpisodeItemViewModel? SelectedEpisode { get; set; }

    [ObservableProperty]
    public partial bool OnlyIndex { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PgcPlayerEpisodeSectionDetailViewModel"/> class.
    /// </summary>
    public PgcPlayerEpisodeSectionDetailViewModel(
        IList<EpisodeInformation> episodes,
        string epid,
        Action<EpisodeInformation> action
    )
    {
        _episodeSelectedAction = action;
        Episodes = episodes
            .Select(p => new EpisodeItemViewModel(p, EpisodeCardStyle.Player))
            .ToList();
        SelectedEpisode = Episodes.FirstOrDefault(p => p.Data.Identifier.Id == epid);
        OnlyIndex = SettingsToolkit.ReadLocalSetting(
            Models.Constants.SettingNames.IsPgcPlayerPartsOnlyIndex,
            false
        );
    }

    /// <inheritdoc/>
    public string Title { get; } = Resources.Episodes;

    [RelayCommand]
    private static Task TryFirstLoadAsync() => Task.CompletedTask;

    [RelayCommand]
    private void SelectEpisode(EpisodeItemViewModel episode)
    {
        SelectedEpisode = episode;
        _episodeSelectedAction?.Invoke(episode.Data);
    }

    partial void OnOnlyIndexChanged(bool value) =>
        SettingsToolkit.WriteLocalSetting(SettingNames.IsPgcPlayerPartsOnlyIndex, value);
}
