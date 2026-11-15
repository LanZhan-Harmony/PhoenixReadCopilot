using System.Globalization;
using Windows.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Pages.Overlay;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 视频项视图模型.
/// </summary>
public sealed partial class VideoItemViewModel : ViewModelBase<VideoInformation>
{
    private readonly Action<VideoItemViewModel>? _removeAction;
    private readonly VideoFavoriteFolder? _favFolder;

    private static readonly ILogger<VideoItemViewModel> _logger = Ioc.Default.GetRequiredService<
        ILogger<VideoItemViewModel>
    >();

    /// <summary>
    /// 上新日历数据实体 (如果是上新日历条目).
    /// </summary>
    public DailyReleaseItem? DailyRelease { get; init; }

    /// <summary>
    /// 是否已正式上线.
    /// </summary>
    public bool IsOnline { get; init; } = true;

    /// <summary>
    /// 上线说明或副标题备注.
    /// </summary>
    public string? ReleaseNote { get; init; }

    /// <summary>
    /// 上线状态展示文案.
    /// </summary>
    public string OnlineStatusText =>
        IsOnline
            ? Strings.Resources.Online
            : (!string.IsNullOrWhiteSpace(ReleaseNote) ? ReleaseNote : Strings.Resources.Upcoming);

    /// <summary>
    /// 是否已预约.
    /// </summary>
    [ObservableProperty]
    public partial bool IsReserved { get; set; }

    /// <summary>
    /// 是否正在操作预约.
    /// </summary>
    [ObservableProperty]
    public partial bool IsReserving { get; set; }

    /// <summary>
    /// 短剧数据实体 (如果是红果短剧).
    /// </summary>
    public Drama? Drama { get; init; }

    /// <summary>
    /// 阅读/观看历史条目 (如果是红果历史记录).
    /// </summary>
    public ReadHistoryItem? HistoryItem { get; init; }

    /// <summary>
    /// 演员数据实体 (如果是红果演员排行榜).
    /// </summary>
    public ActorLeaderboardItem? Actor { get; init; }

    /// <summary>
    /// 演员/用户标识 ID.
    /// </summary>
    public string? ActorId { get; init; }

    /// <summary>
    /// 排行榜名次.
    /// </summary>
    public int? Rank { get; init; }

    /// <summary>
    /// 是否有排名.
    /// </summary>
    public bool HasRank => Rank is > 0;

    /// <summary>
    /// 排名文案 (如 TOP 1).
    /// </summary>
    public string? RankText => HasRank ? $"TOP {Rank}" : null;

    /// <summary>
    /// 纯排名数字字符串.
    /// </summary>
    public string RankDisplay => Rank.HasValue ? Rank.Value.ToString() : string.Empty;

    /// <summary>
    /// 是否为前三名.
    /// </summary>
    public bool IsTopRank => Rank is >= 1 and <= 3;

    /// <summary>
    /// 演员副标题（作品数 · 追剧人数）.
    /// </summary>
    public string ActorSubtitles =>
        Actor?.SubTitles is { Count: > 0 } ? string.Join(" · ", Actor.SubTitles) : string.Empty;

    /// <summary>
    /// 演员代表作.
    /// </summary>
    public string ActorRepresentativeWork =>
        !string.IsNullOrEmpty(Actor?.RepresentativeWork)
            ? (
                Actor.RepresentativeWork.StartsWith("《")
                    ? Actor.RepresentativeWork
                    : $"《{Actor.RepresentativeWork}》"
            )
            : string.Empty;

    /// <summary>
    /// 演员热度/人气文案.
    /// </summary>
    public string ActorPopularity => Actor?.Popularity ?? string.Empty;

    /// <summary>
    /// 样式.
    /// </summary>
    public VideoCardStyle Style { get; }

    /// <summary>
    /// 标题.
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 副标题.
    /// </summary>
    public string Subtitle { get; init; }

    /// <summary>
    /// 封面.
    /// </summary>
    public Uri? Cover { get; init; }

    /// <summary>
    /// 作者 / 分类.
    /// </summary>
    public string Author { get; init; }

    /// <summary>
    /// 评分 (如 "9.5").
    /// </summary>
    public string? Score { get; init; }

    /// <summary>
    /// 是否有评分.
    /// </summary>
    public bool HasScore => !string.IsNullOrWhiteSpace(Score) && Score != "0" && Score != "0.0";

    /// <summary>
    /// 总集数.
    /// </summary>
    public int EpisodeCount { get; init; }

    /// <summary>
    /// 是否有集数或状态.
    /// </summary>
    public bool HasEpisodeCount => EpisodeCount > 0 || !string.IsNullOrWhiteSpace(Subtitle);

    /// <summary>
    /// 集数文本 (如 "全80集" / "更新至50集").
    /// </summary>
    public string? EpisodeCountText => $"{EpisodeCount}集";

    /// <summary>
    /// 标签列表.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// 分类.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// 热度.
    /// </summary>
    public string? Heat { get; init; }

    /// <summary>
    /// 播放量原始文本.
    /// </summary>
    public string? Views { get; init; }

    /// <summary>
    /// 热度或播放量展示文本.
    /// </summary>
    public string? HeatOrPlayCountText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Heat))
            {
                return Heat;
            }

            if (!string.IsNullOrWhiteSpace(Views))
            {
                return Views;
            }

            if (PlayCount is > 0)
            {
                return FormatHelper.FormatCount(Convert.ToInt64(PlayCount.Value));
            }

            return null;
        }
    }

    /// <summary>
    /// 发布时间.
    /// </summary>
    public string? PublishRelativeTime { get; init; }

    /// <summary>
    /// 作者头像.
    /// </summary>
    public Uri? Avatar { get; init; }

    /// <summary>
    /// 播放数.
    /// </summary>
    public double? PlayCount { get; init; }

    /// <summary>
    /// 弹幕数.
    /// </summary>
    public double? DanmakuCount { get; set; }

    /// <summary>
    /// 点赞数.
    /// </summary>
    public double? LikeCount { get; set; }

    /// <summary>
    /// 标签名称.
    /// </summary>
    public string? TagName { get; init; }

    /// <summary>
    /// 收集时间.
    /// </summary>
    public string? CollectTime { get; init; }

    /// <summary>
    /// 进度文本.
    /// </summary>
    public string? ProgressText { get; init; }

    /// <summary>
    /// 用户信息是否有效.
    /// </summary>
    public bool IsUserValid { get; init; }

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
            ProgressText = FormatHelper.FormatDuration(TimeSpan.FromSeconds(progress.Value));
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
    /// Initializes a new instance of the <see cref="VideoItemViewModel"/> class from a <see cref="DailyReleaseItem"/>.
    /// </summary>
    public VideoItemViewModel(
        DailyReleaseItem item,
        Action<VideoItemViewModel>? removeAction = null
    )
        : base(CreateVideoInformation(item.Drama))
    {
        _removeAction = removeAction;
        DailyRelease = item;
        Drama = item.Drama;
        IsOnline = item.IsOnline;
        ReleaseNote = item.Note;
        Style = VideoCardStyle.Calendar;
        Title = item.Drama.Title;

        if (
            !string.IsNullOrEmpty(item.Drama.CoverUrl)
            && Uri.TryCreate(item.Drama.CoverUrl, UriKind.Absolute, out Uri? coverUri)
        )
        {
            Cover = coverUri;
        }

        Score = item.Drama.Score;
        EpisodeCount = item.Drama.EpisodeCount;
        Tags = item.Drama.Tags ?? [];
        Category = item.Categories is { Count: > 0 }
            ? string.Join(" · ", item.Categories)
            : item.Drama.Category;
        Author = !string.IsNullOrEmpty(Category)
            ? Category
            : (!string.IsNullOrEmpty(item.Drama.Category) ? item.Drama.Category : string.Empty);
        TagName = Tags.Count > 0 ? Tags[0] : Category;
        Heat = item.Drama.Heat;
        Views = item.Drama.Views;

        string statusText = item.IsOnline
            ? Strings.Resources.Online
            : (!string.IsNullOrWhiteSpace(item.Note) ? item.Note : Strings.Resources.Upcoming);
        Subtitle = !string.IsNullOrWhiteSpace(item.Note)
            ? statusText
            : (
                item.Drama.EpisodeCount > 0
                    ? $"{statusText} · 全 {item.Drama.EpisodeCount} 集"
                    : statusText
            );
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
    /// Initializes a new instance of the <see cref="VideoItemViewModel"/> class from a <see cref="ReadHistoryItem"/>.
    /// </summary>
    public VideoItemViewModel(
        ReadHistoryItem history,
        Action<VideoItemViewModel>? removeAction = null
    )
        : base(CreateHistoryInformation(history))
    {
        _removeAction = removeAction;
        HistoryItem = history;
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
            ProgressText = FormatHelper.FormatDuration(
                TimeSpan.FromMilliseconds(history.CurrentPositionMs)
            );
        }
    }

    private static VideoInformation CreateHistoryInformation(ReadHistoryItem history)
    {
        AppImage? cover =
            !string.IsNullOrEmpty(history.CoverUrl)
            && Uri.TryCreate(history.CoverUrl, UriKind.Absolute, out Uri? uri)
                ? new AppImage(uri)
                : null;
        return new VideoInformation(
            new MediaIdentifier(history.DramaId, history.Title, cover),
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

        if (HistoryItem is not null)
        {
            ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), HistoryItem.DramaId);
            return;
        }

        if (DailyRelease is not null && !IsOnline)
        {
            _ = ToggleReservationAsync();
            return;
        }

        ItemServices.Navigation.NavigateToOver(
            typeof(VideoPlayerPage),
            Drama ?? (object)new VideoSnapshot(Data)
        );
    }

    /// <summary>
    /// 切换预约状态.
    /// </summary>
    [RelayCommand]
    private async Task ToggleReservationAsync()
    {
        if (IsReserving)
        {
            return;
        }

        string? dramaId = Drama?.Id;
        if (string.IsNullOrEmpty(dramaId))
        {
            return;
        }

        IsReserving = true;
        try
        {
            bool targetActive = !IsReserved;
            PhoenixOperationResult result = await ItemServices.PhoenixRead.SetReservationAsync(
                dramaId,
                targetActive
            );

            if (result.Succeeded)
            {
                IsReserved = targetActive;
                string msg = targetActive
                    ? Strings.Resources.ReserveSucceed
                    : Strings.Resources.CancelReserveSucceed;
                ItemServices.App.ShowTipCommand.Execute((msg, InfoType.Success));
            }
            else
            {
                string msg = !string.IsNullOrEmpty(result.Message)
                    ? result.Message
                    : Strings.Resources.ReserveFailed;
                ItemServices.App.ShowTipCommand.Execute((msg, InfoType.Warning));
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"操作剧集预约状态失败: {dramaId}");
            ItemServices.App.ShowTipCommand.Execute(
                (Strings.Resources.ReserveFailed, InfoType.Error)
            );
        }
        finally
        {
            IsReserving = false;
        }
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
            if (HistoryItem is not null)
            {
                await ItemServices.PhoenixRead.DeleteHistoryAsync(
                    HistoryItem.DramaId,
                    HistoryItem.VideoId
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
