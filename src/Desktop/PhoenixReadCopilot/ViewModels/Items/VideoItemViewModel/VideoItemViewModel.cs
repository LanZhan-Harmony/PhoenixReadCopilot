using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Toolkits;
using Windows.Globalization;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 视频项视图模型.
/// </summary>
public sealed partial class VideoItemViewModel : ViewModelBase<VideoInformation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VideoItemViewModel"/> class.
    /// </summary>
    public VideoItemViewModel(
        VideoInformation info,
        VideoCardStyle style,
        Action<VideoItemViewModel> removeAction = null,
        VideoFavoriteFolder? favFolder = null
    )
        : base(info)
    {
        _removeAction = removeAction;
        _favFolder = favFolder;
        string primaryLan = ApplicationLanguages.Languages[0];
        Style = style;
        Title = info.Identifier.Title;
        Cover = info.Identifier.Cover?.Uri;
        Author = info.Publisher?.User?.Name;
        Avatar = info.Publisher?.User?.Avatar?.Uri;
        PublishRelativeTime = info.PublishTime?.Humanize(culture: new CultureInfo(primaryLan));
        PlayCount = info.CommunityInformation?.PlayCount;
        DanmakuCount = info.CommunityInformation?.DanmakuCount;
        LikeCount = info.CommunityInformation?.LikeCount;
        TagName = info.GetExtensionIfNotNull<string?>(VideoExtensionDataId.TagName);
        Subtitle = info.GetExtensionIfNotNull<string?>(VideoExtensionDataId.Subtitle);
        CollectTime = info.GetExtensionIfNotNull<DateTimeOffset>(VideoExtensionDataId.CollectTime)
            .Humanize(null, new CultureInfo("zh-CN"));
        IsUserValid = info.Publisher?.User is not null;
        int? progress = info.GetExtensionIfNotNull<int?>(VideoExtensionDataId.Progress);
        if (progress is not null)
        {
            ProgressText = AppToolkit.FormatDuration(TimeSpan.FromSeconds(progress.Value));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoItemViewModel"/> class from a <see cref="Drama"/>.
    /// </summary>
    public VideoItemViewModel(
        Drama drama,
        VideoCardStyle style = VideoCardStyle.Recommend,
        Action<VideoItemViewModel>? removeAction = null,
        VideoFavoriteFolder? favFolder = null
    )
        : base(CreateVideoInformation(drama))
    {
        _removeAction = removeAction;
        _favFolder = favFolder;
        Drama = drama;
        Style = style;
        Title = drama.Title;
        Subtitle = drama.Remark;

        if (
            !string.IsNullOrEmpty(drama.CoverUrl)
            && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out Uri? coverUri)
        )
        {
            Cover = coverUri;
        }

        Score = drama.Score;
        EpisodeCount = drama.EpisodeCount;
        Tags = drama.Tags ?? [];
        Category = drama.Category;
        Heat = drama.Heat;
        Views = drama.Views;

        Author = !string.IsNullOrEmpty(drama.Category)
            ? drama.Category
            : (drama.Credits.FirstOrDefault()?.User?.Name ?? string.Empty);
        TagName = Tags.Count > 0 ? Tags[0] : drama.Category;
        if (double.TryParse(drama.Views, out double vc))
        {
            PlayCount = vc;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoItemViewModel"/> class from an <see cref="ActorLeaderboardItem"/>.
    /// </summary>
    public VideoItemViewModel(
        ActorLeaderboardItem actor,
        Action<VideoItemViewModel>? removeAction = null
    )
        : base(CreateActorInformation(actor))
    {
        _removeAction = removeAction;
        Actor = actor;
        ActorId = !string.IsNullOrEmpty(actor.UserId) ? actor.UserId : actor.ActorId;
        Style = VideoCardStyle.Rank;
        Title = actor.Name;
        Rank = actor.Position;
        Author = !string.IsNullOrEmpty(actor.RepresentativeWork)
            ? $"代表作: 《{actor.RepresentativeWork}》"
            : "短剧演员";

        string trendText = actor.RankChangeType switch
        {
            1 => $"↑ 上升 {actor.RankChangeValue}",
            2 => $"↓ 下降 {actor.RankChangeValue}",
            _ => "— 排名持平",
        };
        string? subtitles = actor.SubTitles is { Count: > 0 }
            ? string.Join(" · ", actor.SubTitles)
            : null;
        Subtitle = subtitles is not null ? $"{subtitles} ({trendText})" : trendText;

        if (
            !string.IsNullOrEmpty(actor.AvatarUrl)
            && Uri.TryCreate(actor.AvatarUrl, UriKind.Absolute, out Uri? uri)
        )
        {
            Cover = uri;
            Avatar = uri;
        }

        Heat = actor.Popularity;
        TagName = actor.Popularity;
        IsUserValid = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoItemViewModel"/> class from a <see cref="ReadHistoryEntry"/>.
    /// </summary>
    public VideoItemViewModel(
        ReadHistoryEntry history,
        Action<VideoItemViewModel>? removeAction = null
    )
        : base(CreateHistoryInformation(history))
    {
        _removeAction = removeAction;
        HistoryEntry = history;
        Style = VideoCardStyle.History;
        Title = history.Title;
        Subtitle = history.EpisodeNumber > 0 ? $"第 {history.EpisodeNumber} 集" : string.Empty;

        if (
            !string.IsNullOrEmpty(history.CoverUrl)
            && Uri.TryCreate(history.CoverUrl, UriKind.Absolute, out Uri? coverUri)
        )
        {
            Cover = coverUri;
        }

        if (history.CurrentPositionMs > 0)
        {
            ProgressText = AppToolkit.FormatDuration(
                TimeSpan.FromMilliseconds(history.CurrentPositionMs)
            );
        }
    }

    private static VideoInformation CreateHistoryInformation(ReadHistoryEntry history)
    {
        AppImage? cover =
            !string.IsNullOrEmpty(history.CoverUrl)
            && Uri.TryCreate(history.CoverUrl, UriKind.Absolute, out Uri? uri)
                ? new AppImage(uri)
                : null;
        return new VideoInformation(
            new MediaIdentifier(history.SeriesId, history.Title, cover),
            null
        );
    }

    private static VideoInformation CreateActorInformation(ActorLeaderboardItem actor)
    {
        AppImage? cover =
            !string.IsNullOrEmpty(actor.AvatarUrl)
            && Uri.TryCreate(actor.AvatarUrl, UriKind.Absolute, out Uri? uri)
                ? new AppImage(uri)
                : null;
        string id = !string.IsNullOrEmpty(actor.ActorId) ? actor.ActorId : actor.UserId;
        return new VideoInformation(new MediaIdentifier(id, actor.Name, cover), null);
    }

    private static VideoInformation CreateVideoInformation(Drama drama)
    {
        AppImage? cover =
            !string.IsNullOrEmpty(drama.CoverUrl)
            && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out Uri? uri)
                ? new AppImage(uri)
                : null;
        return new VideoInformation(new MediaIdentifier(drama.Id, drama.Title, cover), null);
    }

    [RelayCommand]
    private void ShowUserSpace()
    {
        if (Actor is not null)
        {
            ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Actor);
            return;
        }

        string? targetId = !string.IsNullOrEmpty(ActorId) ? ActorId : null;
        if (!string.IsNullOrEmpty(targetId))
        {
            ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), targetId);
        }
    }

    [RelayCommand]
    private void Play()
    {
        if (Actor is not null || !string.IsNullOrEmpty(ActorId))
        {
            ShowUserSpace();
            return;
        }

        PlayerDisplayMode preferDisplayMode = ItemServices.Settings.DefaultPlayerDisplayMode;
        if (preferDisplayMode == PlayerDisplayMode.NewWindow)
        {
            OpenInNewWindowCommand.Execute(null);
            return;
        }

        PlayerType preferPlayer = ItemServices.Settings.PlayerType;
        if (preferPlayer == PlayerType.Web)
        {
            return;
        }

        if (HistoryEntry is not null)
        {
            ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), HistoryEntry.SeriesId);
            return;
        }

        ItemServices.Navigation.NavigateToOver(
            typeof(VideoPlayerPage),
            Drama ?? (object)new VideoSnapshot(Data)
        );
    }

    [RelayCommand]
    private async Task AddToViewLaterAsync()
    {
        try
        {
            if (Drama is not null)
            {
                await ItemServices.LocalViewLater.AddAsync(Drama);
            }
            else if (Data is not null)
            {
                Drama drama = new()
                {
                    Id = Data.Identifier?.Id ?? string.Empty,
                    Title = Data.Identifier?.Title ?? string.Empty,
                    CoverUrl = Data.Identifier?.Cover?.Uri?.ToString() ?? string.Empty,
                };
                await ItemServices.LocalViewLater.AddAsync(drama);
            }
            ItemServices.App.ShowTipCommand.Execute(
                (Strings.Resources.AddViewLaterSucceed, InfoType.Success)
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"添加稍后再看失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Strings.Resources.AddViewLaterFailed, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private async Task RemoveViewLaterAsync()
    {
        try
        {
            if (Drama is not null)
            {
                await ItemServices.LocalViewLater.RemoveAsync(Drama.Id);
            }
            else if (Data is not null && !string.IsNullOrEmpty(Data.Identifier?.Id))
            {
                await ItemServices.LocalViewLater.RemoveAsync(Data.Identifier.Id);
            }
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"移除稍后再看视频失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Strings.Resources.FailedToRemoveVideoFromViewLater, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private async Task RemoveHistoryAsync()
    {
        try
        {
            if (HistoryEntry is not null)
            {
                await ItemServices.PhoenixRead.DeleteHistoryAsync(
                    HistoryEntry.SeriesId,
                    HistoryEntry.VideoId
                );
            }
            else if (Data is not null && !string.IsNullOrEmpty(Data.Identifier?.Id))
            {
                await ItemServices.PhoenixRead.DeleteHistoryAsync(Data.Identifier.Id, string.Empty);
            }
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"移除历史记录视频失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Strings.Resources.FailedToRemoveVideoFromHistory, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync()
    {
        try
        {
            if (Drama is not null)
            {
                await ItemServices.PhoenixRead.SetFavoriteAsync(
                    Drama.Id,
                    string.Empty,
                    active: false
                );
            }
            else if (Data is not null && !string.IsNullOrEmpty(Data.Identifier?.Id))
            {
                await ItemServices.PhoenixRead.SetFavoriteAsync(
                    Data.Identifier.Id,
                    string.Empty,
                    active: false
                );
            }
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"移除收藏视频失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Strings.Resources.FailedToRemoveVideoFromFavorite, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private void OpenInNewWindow() => new PlayerWindow().OpenVideo(new VideoSnapshot(Data));

    [RelayCommand]
    private void Pin()
    {
        string coverUri = Cover?.ToString()
            ?? Data.Identifier.Cover?.Uri?.ToString()
            ?? string.Empty;
        PinItem pinItem = new(
            Data.Identifier.Id,
            Data.Identifier.Title,
            coverUri,
            PinContentType.Video
        );
        ItemServices.Pinner.AddItemCommand.Execute(pinItem);
    }
}
