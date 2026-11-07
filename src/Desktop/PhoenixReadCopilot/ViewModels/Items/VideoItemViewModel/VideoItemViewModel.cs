using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.Toolkits;
using Richasy.BiliKernel.Models.Appearance;
using Richasy.BiliKernel.Models.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Globalization;
using Windows.System;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

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
        Action<VideoItemViewModel> removeAction = default,
        VideoFavoriteFolder? favFolder = default
    )
        : base(info)
    {
        _removeAction = removeAction;
        _favFolder = favFolder;
        var primaryLan = ApplicationLanguages.Languages[0];
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
            .Humanize(default, new CultureInfo("zh-CN"));
        IsUserValid = info.Publisher?.User is not null;
        var progress = info.GetExtensionIfNotNull<int?>(VideoExtensionDataId.Progress);
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
        Action<VideoItemViewModel>? removeAction = default
    )
        : base(CreateVideoInformation(drama))
    {
        _removeAction = removeAction;
        Drama = drama;
        Style = style;
        Title = drama.Title;
        Subtitle = drama.Remark;

        if (
            !string.IsNullOrEmpty(drama.CoverUrl)
            && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out var coverUri)
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
        if (double.TryParse(drama.Views, out var vc))
        {
            PlayCount = vc;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoItemViewModel"/> class from an <see cref="ActorLeaderboardItem"/>.
    /// </summary>
    public VideoItemViewModel(
        ActorLeaderboardItem actor,
        Action<VideoItemViewModel>? removeAction = default
    )
        : base(CreateActorInformation(actor))
    {
        _removeAction = removeAction;
        Actor = actor;
        ActorId = !string.IsNullOrEmpty(actor.ActorId) ? actor.ActorId : actor.UserId;
        Style = VideoCardStyle.Rank;
        Title = actor.Name;
        Rank = actor.Position;
        Author = !string.IsNullOrEmpty(actor.RepresentativeWork) ? $"代表作: 《{actor.RepresentativeWork}》" : "短剧演员";

        string trendText = actor.RankChangeType switch
        {
            1 => $"↑ 上升 {actor.RankChangeValue}",
            2 => $"↓ 下降 {actor.RankChangeValue}",
            _ => "— 排名持平",
        };
        var subtitles = actor.SubTitles is { Count: > 0 } ? string.Join(" · ", actor.SubTitles) : null;
        Subtitle = subtitles != null ? $"{subtitles} ({trendText})" : trendText;

        if (!string.IsNullOrEmpty(actor.AvatarUrl) && Uri.TryCreate(actor.AvatarUrl, UriKind.Absolute, out var uri))
        {
            Cover = uri;
            Avatar = uri;
        }

        Heat = actor.Popularity;
        TagName = actor.Popularity;
        IsUserValid = true;
    }

    private static VideoInformation CreateActorInformation(ActorLeaderboardItem actor)
    {
        var cover = !string.IsNullOrEmpty(actor.AvatarUrl) && Uri.TryCreate(actor.AvatarUrl, UriKind.Absolute, out var uri)
            ? new BiliImage(uri)
            : default;
        var id = !string.IsNullOrEmpty(actor.ActorId) ? actor.ActorId : actor.UserId;
        return new VideoInformation(new MediaIdentifier(id, actor.Name, cover), default);
    }

    private static VideoInformation CreateVideoInformation(Drama drama)
    {
        var cover =
            !string.IsNullOrEmpty(drama.CoverUrl)
            && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out var uri)
                ? new BiliImage(uri)
                : default;
        return new VideoInformation(new MediaIdentifier(drama.Id, drama.Title, cover), default);
    }

    [RelayCommand]
    private void Play()
    {
        if (Actor is not null)
        {
            ShowUserSpace();
            return;
        }

        var preferDisplayMode = ItemServices.Settings.DefaultPlayerDisplayMode;
        if (preferDisplayMode == PlayerDisplayMode.NewWindow)
        {
            OpenInNewWindowCommand.Execute(default);
            return;
        }

        var preferPlayer = ItemServices.Settings.PlayerType;
        if (preferPlayer == PlayerType.Web)
        {
            ItemServices.Navigation.NavigateToOver(typeof(WebPlayerPage), GetWebUri().ToString());
            return;
        }

        ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), Drama ?? (object)new VideoSnapshot(Data));
    }

    [RelayCommand]
    private void PlayInPrivate() =>
        ItemServices.Navigation.NavigateToOver(
            typeof(VideoPlayerPage),
            new VideoSnapshot(Data, true)
        );

    [RelayCommand]
    private void ShowUserSpace()
    {
        if (!string.IsNullOrEmpty(ActorId))
        {
            ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), ActorId);
            return;
        }

        if (Data.Publisher?.User is not null)
        {
            ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Data.Publisher.User);
        }
    }

    [RelayCommand]
    private async Task AddToViewLaterAsync()
    {
        try
        {
            await ItemServices.ViewLater.AddAsync(Data.Identifier.Id);
            ItemServices.App.ShowTipCommand.Execute(
                (Resources.AddViewLaterSucceed, InfoType.Success)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加稍后再看失败");
            ItemServices.App.ShowTipCommand.Execute((Resources.AddViewLaterFailed, InfoType.Error));
        }
    }

    [RelayCommand]
    private async Task RemoveViewLaterAsync()
    {
        try
        {
            await ItemServices.ViewLater.RemoveAsync([Data.Identifier.Id]);
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除稍后再看视频失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Resources.FailedToRemoveVideoFromViewLater, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private async Task RemoveHistoryAsync()
    {
        try
        {
            await ItemServices.ViewHistory.RemoveVideoHistoryItemAsync(Data);
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除历史记录视频失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Resources.FailedToRemoveVideoFromHistory, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync()
    {
        try
        {
            await ItemServices.Favorite.RemoveVideoAsync(_favFolder, Data.Identifier);
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除收藏视频失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Resources.FailedToRemoveVideoFromFavorite, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private async Task OpenInBroswerAsync() => await Launcher.LaunchUriAsync(GetWebUri()).AsTask();

    [RelayCommand]
    private void OpenInNewWindow() => new PlayerWindow().OpenVideo(new VideoSnapshot(Data));

    [RelayCommand]
    private void CopyUri()
    {
        var dp = new DataPackage();
        dp.SetText(GetWebUri().ToString());
        dp.SetWebLink(GetWebUri());
        Clipboard.SetContent(dp);
        ItemServices.App.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
    }

    [RelayCommand]
    private void Pin()
    {
        var coverUri = Cover?.ToString() ?? Data.Identifier.Cover?.Uri?.ToString() ?? string.Empty;
        var pinItem = new PinItem(
            Data.Identifier.Id,
            Data.Identifier.Title,
            coverUri,
            PinContentType.Video
        );
        ItemServices.Pinner.AddItemCommand.Execute(pinItem);
    }

    private Uri GetWebUri()
    {
        if (Drama is not null)
        {
            return new Uri($"https://hongguoduanju.com/drama/{Drama.Id}");
        }

        var shortLink = Data.GetExtensionIfNotNull<string>(VideoExtensionDataId.ShortLink);
        return string.IsNullOrEmpty(shortLink)
            ? new Uri($"https://www.bilibili.com/video/av{Data.Identifier.Id}")
            : new Uri(shortLink);
    }
}
