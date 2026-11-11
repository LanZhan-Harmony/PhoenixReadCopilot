using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Appearance;
using Richasy.BiliKernel.Models.Media;
using Windows.System;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 剧集条目视图模型.
/// </summary>
public sealed partial class SeasonItemViewModel : ViewModelBase<SeasonInformation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonItemViewModel"/> class.
    /// </summary>
    public SeasonItemViewModel(
        SeasonInformation data,
        SeasonCardStyle style,
        PgcFavoriteStatus? favStatus = null,
        Action<SeasonItemViewModel>? removeAction = null
    )
        : base(data)
    {
        Title = data.Identifier.Title;
        Cover = data.Identifier.Cover.Uri;
        Style = style;
        Subtitle = data.GetExtensionIfNotNull<string>(SeasonExtensionDataId.Subtitle);
        Highlight = data.GetExtensionIfNotNull<string>(SeasonExtensionDataId.Highlight);
        Score = data.GetExtensionIfNotNull<double>(SeasonExtensionDataId.Score);
        InWantWatch = favStatus == PgcFavoriteStatus.Want;
        InWatching = favStatus == PgcFavoriteStatus.Watching;
        InWatched = favStatus == PgcFavoriteStatus.Watched;
        _removeAction = removeAction;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonItemViewModel"/> class from a <see cref="DailyReleaseItem"/>.
    /// </summary>
    public SeasonItemViewModel(
        DailyReleaseItem item,
        SeasonCardStyle style = SeasonCardStyle.Timeline,
        Action<SeasonItemViewModel>? removeAction = null
    )
        : base(
            CreateSeasonInformation(
                item.Drama,
                !string.IsNullOrWhiteSpace(item.Note)
                    ? item.Note
                    : (item.IsOnline ? "已上线" : "待播"),
                item.IsOnline ? "已上线" : "预告"
            )
        )
    {
        Drama = item.Drama;
        _removeAction = removeAction;
        Style = style;
        Title = item.Drama.Title;
        if (
            !string.IsNullOrEmpty(item.Drama.CoverUrl)
            && Uri.TryCreate(item.Drama.CoverUrl, UriKind.Absolute, out Uri? uri)
        )
        {
            Cover = uri;
        }
        string status = item.IsOnline ? "已上线" : "待播";
        Subtitle = !string.IsNullOrWhiteSpace(item.Note)
            ? $"{status} · {item.Note}"
            : (
                item.Drama.EpisodeCount > 0 ? $"{status} · 全 {item.Drama.EpisodeCount} 集" : status
            );
        Highlight = item.IsOnline ? "已上线" : "预告";
        Score =
            double.TryParse(item.Drama.Score, out double parsedScore) && parsedScore > 0
                ? parsedScore
                : null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonItemViewModel"/> class from a <see cref="Drama"/>.
    /// </summary>
    public SeasonItemViewModel(
        Drama drama,
        SeasonCardStyle style = SeasonCardStyle.Index,
        Action<SeasonItemViewModel>? removeAction = null
    )
        : base(
            CreateSeasonInformation(
                drama,
                drama.Remark,
                !string.IsNullOrWhiteSpace(drama.Score) ? $"{drama.Score}分" : null
            )
        )
    {
        Drama = drama;
        _removeAction = removeAction;
        Style = style;
        Title = drama.Title;
        if (
            !string.IsNullOrEmpty(drama.CoverUrl)
            && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out Uri? uri)
        )
        {
            Cover = uri;
        }
        Subtitle = !string.IsNullOrWhiteSpace(drama.Remark)
            ? drama.Remark
            : (
                drama.EpisodeCount > 0
                    ? $"{drama.Category} · 全 {drama.EpisodeCount} 集"
                    : drama.Category
            );
        Highlight = !string.IsNullOrWhiteSpace(drama.Score)
            ? $"{drama.Score}分"
            : (drama.Tags.Count > 0 ? drama.Tags[0] : null);
        Score =
            double.TryParse(drama.Score, out double parsedScore) && parsedScore > 0
                ? parsedScore
                : null;
    }

    private static SeasonInformation CreateSeasonInformation(
        Drama drama,
        string? subtitle = null,
        string? highlight = null
    )
    {
        BiliImage? cover =
            !string.IsNullOrEmpty(drama.CoverUrl)
            && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out Uri? uri)
                ? new BiliImage(uri)
                : null;
        return new SeasonInformation(new MediaIdentifier(drama.Id, drama.Title, cover));
    }

    [RelayCommand]
    private void Play()
    {
        PlayerDisplayMode preferDisplayMode = ItemServices.Settings.DefaultPlayerDisplayMode;
        if (preferDisplayMode == PlayerDisplayMode.NewWindow)
        {
            OpenInNewWindowCommand.Execute(null);
            return;
        }

        PlayerType preferPlayer = ItemServices.Settings.PlayerType;
        if (preferPlayer == PlayerType.Web)
        {
            ItemServices.Navigation.NavigateToOver(typeof(WebPlayerPage), GetWebUrl());
            return;
        }

        if (Drama is not null)
        {
            ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), Drama);
            return;
        }

        ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), Data.Identifier.Id);
    }

    [RelayCommand]
    private Task OpenInBroswerAsync() => Launcher.LaunchUriAsync(new Uri(GetWebUrl())).AsTask();

    [RelayCommand]
    private void OpenInNewWindow() =>
        new PlayerWindow().OpenPgc(
            new MediaIdentifier("ss_" + Data.Identifier.Id, null, null)
        );

    [RelayCommand]
    private async Task FollowAsync()
    {
        try
        {
            await ItemServices.Entertainment.FollowAsync(Data.Identifier.Id);
            ItemServices.App.ShowTipCommand.Execute((Resources.Followed, InfoType.Success));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "追番/追剧失败");
            ItemServices.App.ShowTipCommand.Execute((Resources.FailedToFollowPgc, InfoType.Error));
        }
    }

    [RelayCommand]
    private async Task UnfollowAsync()
    {
        try
        {
            await ItemServices.Entertainment.UnfollowAsync(Data.Identifier.Id);
            ItemServices.App.ShowTipCommand.Execute((Resources.Unfollowed, InfoType.Success));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "取消追番/追剧失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Resources.FailedToUnfollowPgc, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private Task MarkWantWatchAsync() => MarkFavoriteStatusAsync(PgcFavoriteStatus.Want);

    [RelayCommand]
    private Task MarkWatchingAsync() => MarkFavoriteStatusAsync(PgcFavoriteStatus.Watching);

    [RelayCommand]
    private Task MarkWatchedAsync() => MarkFavoriteStatusAsync(PgcFavoriteStatus.Watched);

    [RelayCommand]
    private void Pin()
    {
        PinItem pinItem = new(
            $"ss_{Data.Identifier.Id}",
            Data.Identifier.Title,
            Data.Identifier.Cover.Uri.ToString(),
            PinContentType.Pgc
        );
        ItemServices.Pinner.AddItemCommand.Execute(pinItem);
    }

    private string GetWebUrl() => $"https://www.bilibili.com/bangumi/play/ss{Data.Identifier.Id}";

    private async Task MarkFavoriteStatusAsync(PgcFavoriteStatus status)
    {
        try
        {
            await ItemServices.Favorite.MarkPgcAsync(Data.Identifier, status);
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "标记想看失败");
            ItemServices.App.ShowTipCommand.Execute((Resources.FailedToMarkPgc, InfoType.Error));
        }
    }
}
