using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// PGC 播放器分集详情视图模型.
/// </summary>
public sealed partial class PgcPlayerEpisodeSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    private readonly Action<EpisodeInformation> _episodeSelectedAction;
    private readonly AppSettings _settings;

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
        Action<EpisodeInformation> action,
        ISettingsService settingsService
    )
    {
        _settings = settingsService.Settings;
        _episodeSelectedAction = action;
        Episodes =
        [
            .. episodes
                .Select(p => new EpisodeItemViewModel(p, EpisodeCardStyle.Player)),
        ];
        SelectedEpisode = Episodes.FirstOrDefault(p => p.Data.Identifier.Id == epid);
        OnlyIndex = _settings.IsPgcPlayerPartsOnlyIndex;
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

    partial void OnOnlyIndexChanged(bool value) => _settings.IsPgcPlayerPartsOnlyIndex = value;
}
