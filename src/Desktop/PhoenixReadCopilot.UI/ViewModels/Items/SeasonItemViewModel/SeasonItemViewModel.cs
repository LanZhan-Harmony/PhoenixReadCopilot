using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Forms;
using PhoenixReadCopilot.UI.Models;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages.Overlay;
using PhoenixReadCopilot.UI.Strings;
using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Media;
using Windows.System;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Items;

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
        PgcFavoriteStatus? favStatus = default,
        Action<SeasonItemViewModel>? removeAction = default
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

    [RelayCommand]
    private void Play()
    {
        var preferDisplayMode = ItemServices.Settings.DefaultPlayerDisplayMode;
        if (preferDisplayMode == PlayerDisplayMode.NewWindow)
        {
            OpenInNewWindowCommand.Execute(default);
            return;
        }

        var preferPlayer = ItemServices.Settings.PlayerType;
        if (preferPlayer == PlayerType.Web)
        {
            ItemServices.Navigation.NavigateToOver(typeof(WebPlayerPage), GetWebUrl());
            return;
        }

        var identifier = new MediaIdentifier("ss_" + Data.Identifier.Id, default, default);
        ItemServices.Navigation.NavigateToOver(typeof(PgcPlayerPage), identifier);
    }

    [RelayCommand]
    private Task OpenInBroswerAsync() => Launcher.LaunchUriAsync(new Uri(GetWebUrl())).AsTask();

    [RelayCommand]
    private void OpenInNewWindow() =>
        new PlayerWindow().OpenPgc(
            new MediaIdentifier("ss_" + Data.Identifier.Id, default, default)
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
        var pinItem = new PinItem(
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
