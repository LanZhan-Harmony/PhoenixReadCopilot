using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Appearance;
using Richasy.BiliKernel.Models.Media;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// PGC 播放器页面视图模型.
/// </summary>
public sealed partial class PgcPlayerPageViewModel
{
    private void InitializeView(PgcPlayerView view)
    {
        _view = view;
        _type = view.Information.GetExtensionIfNotNull<EntertainmentType>(
            SeasonExtensionDataId.PgcType
        );
        Cover = view.Information.Identifier.Cover.SourceUri;
        SeasonTitle = view.Information.Identifier.Title;
        Description = view.Information.GetExtensionIfNotNull<string>(
            SeasonExtensionDataId.Description
        );
        Alias = view.Information.GetExtensionIfNotNull<string>(SeasonExtensionDataId.Alias);
        if (string.IsNullOrEmpty(Alias))
        {
            Alias = view.Information.GetExtensionIfNotNull<string>(SeasonExtensionDataId.Subtitle);
        }

        SeasonId = view.Information.Identifier.Id;
        IsFollow = view.Information.IsTracking ?? false;
        PlayCount = view.Information.CommunityInformation.PlayCount ?? 0;
        DanmakuCount = view.Information.CommunityInformation.DanmakuCount ?? 0;
        CommentCount = view.Information.CommunityInformation.CommentCount ?? 0;
        LikeCount = view.Information.CommunityInformation.LikeCount ?? 0;
        CoinCount = view.Information.CommunityInformation.CoinCount ?? 0;
        FavoriteCount = view.Information.CommunityInformation.FavoriteCount ?? 0;
        FollowCount = view.Information.CommunityInformation.TrackCount ?? 0;
    }

    private void InitializeDash(DashMediaInformation info)
    {
        _videoSegments = info.Videos;
        _audioSegments = info.Audios;
        Downloader.Clear();
        // Formats = info.Formats.Select(p => new PlayerFormatItemViewModel(new PhoenixReadCopilot.Core.Models.PlayerFormatInformation(p.Quality, p.Description, p.NeedVip, p.NeedLogin))).ToList();

        PreferQualityType preferFormatSetting = _settings.PreferQuality;
        List<PlayerFormatItemViewModel> availableFormats = Formats.Where(p => p.IsEnabled).ToList();
        PlayerFormatItemViewModel? selectedFormat = null;
        if (preferFormatSetting == PreferQualityType.Auto)
        {
            int lastSelectedFormat = _settings.LastSelectedPgcQuality;
            selectedFormat = availableFormats.Find(p => p.Data.Quality == lastSelectedFormat);
        }
        else if (preferFormatSetting == PreferQualityType.UHD)
        {
            selectedFormat = availableFormats.Find(p => p.Data.Quality == 120);
        }
        else if (preferFormatSetting == PreferQualityType.HD)
        {
            selectedFormat = availableFormats.Find(p =>
                p.Data.Quality == 116 || p.Data.Quality == 80
            );
        }

        if (selectedFormat is null)
        {
            int maxQuality = availableFormats.Max(p => p.Data.Quality);
            selectedFormat = availableFormats.Find(p => p.Data.Quality == maxQuality);
        }

        int episodeIndex = _view.Episodes.IndexOf(_episode);
        /*Downloader.InitializeMetas(
            GetEpisodeUrl(),
            GetSeasonUrl(),
            info.Formats.AsReadOnly(),
            _view.Episodes?.Count > 1 ? _view.Episodes.AsReadOnly() : default,
            episodeIndex
        );*/

        ChangeFormatCommand.Execute(selectedFormat);
    }

    private void InitializeSections()
    {
        if (Sections?.Count > 0)
        {
            return;
        }

        List<IPlayerSectionDetailViewModel> sections =
        [
            new PgcPlayerInfoSectionDetailViewModel(this),
        ];

        if (_view.Episodes?.Count > 1)
        {
            sections.Add(
                new PgcPlayerEpisodeSectionDetailViewModel(
                    _view.Episodes,
                    _episode.Identifier.Id,
                    ChangeEpisode,
                    _settingsService
                )
            );
        }

        if (_view.Seasons?.Count > 1)
        {
            sections.Add(
                new PgcPlayerSeasonSectionDetailViewModel(
                    _view.Seasons,
                    _view.Information.Identifier.Id
                )
            );
        }

        sections.Add(_comments);

        Sections = sections;
        SelectSection(Sections.First());
        SectionInitialized?.Invoke(this, EventArgs.Empty);
    }

    private void ClearView()
    {
        IsPageLoadFailed = false;
        _view = null;
        _videoSegments = null;
        _audioSegments = null;
        _initialProgress = -1;
        Cover = null;
        SeasonTitle = null;
        IsFollow = false;
        PlayCount = 0;
        DanmakuCount = 0;
        CommentCount = 0;
        LikeCount = 0;
        CoinCount = 0;
        FavoriteCount = 0;
        FollowCount = 0;
        IsLiked = false;
        IsCoined = false;
        IsFavorited = false;
        IsCoinAlsoLike = true;
        FavoriteFolders = null;
        HasNextEpisode = false;

        Formats = null;
        SelectedFormat = null;

        Sections = null;
        SelectedSection = null;
    }

    private EpisodeInformation? FindInitialEpisode(string? initialEpisodeId)
    {
        EpisodeInformation? playEpisode = null;
        if (!string.IsNullOrEmpty(initialEpisodeId))
        {
            playEpisode = _view.Episodes.FirstOrDefault(p => p.Identifier.Id == initialEpisodeId);
        }

        if (playEpisode == null)
        {
            string? historyEpisodeId = _view.Progress?.Cid;
            bool autoLoadHistory = _settings.AutoLoadHistory;
            if (!string.IsNullOrEmpty(historyEpisodeId) && autoLoadHistory)
            {
                playEpisode = _view.Episodes.FirstOrDefault(p =>
                    p.Identifier.Id == historyEpisodeId
                );
            }
        }

        return playEpisode ?? _view.Episodes.FirstOrDefault();
    }

    private object? FindNextEpisode()
    {
        if (Sections is null)
        {
            return null;
        }

        if (
            Sections.OfType<PgcPlayerEpisodeSectionDetailViewModel>().FirstOrDefault()
            is PgcPlayerEpisodeSectionDetailViewModel episodeSection
        )
        {
            int index = episodeSection.Episodes.Select(p => p.Data).ToList().IndexOf(_episode);
            if (index < episodeSection.Episodes.Count - 1)
            {
                return episodeSection.Episodes.ElementAt(index + 1).Data;
            }
        }

        return null;
    }

    private void InitializeNextEpisode()
    {
        object? next = FindNextEpisode();
        HasNextEpisode = next is not null;
        if (next is EpisodeInformation part)
        {
            NextEpisodeTip = string.Format(
                Resources.PlayNextEpisodeTipTemplate,
                part.Identifier.Title
            );
        }
        else if (next is VideoInformation video)
        {
            NextEpisodeTip = string.Format(
                Resources.PlayNextVideoTipTemplate,
                video.Identifier.Title
            );
        }
    }

    private void LoadInitialProgress(int? progress = null)
    {
        _initialProgress = 0;
        if (progress is not null)
        {
            _initialProgress = progress.Value;
        }
        else if (_view.Progress is not null && _view.Progress.Cid == _episode.Identifier.Id)
        {
            int p = Convert.ToInt32(_view.Progress.Progress);
            if (p < _episode.Duration - 5)
            {
                _initialProgress = p;
            }
        }
    }

    private void CalcPlayerHeight()
    {
        if (PlayerWidth <= 0 || Player.IsFullScreen || Player.IsCompactOverlay || _episode is null)
        {
            return;
        }

        MediaAspectRatio ratio = _episode.GetExtensionIfNotNull<MediaAspectRatio>(
            EpisodeExtensionDataId.AspectRatio
        );
        if (ratio.Width == 0)
        {
            ratio = new MediaAspectRatio(16, 9);
        }

        PlayerHeight = (double)(PlayerWidth * ratio.Height / ratio.Width);
    }

    private void ReloadEpisode()
    {
        if (_episode is null)
        {
            return;
        }

        if (Player.Position > 0)
        {
            _injectedProgress = Player.Position;
        }

        ChangeEpisode(_episode);
    }
}
