using CommunityToolkit.Mvvm.Input;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Toolkits;
using Richasy.BiliKernel.Models.Media;
using Windows.System;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 单集条目视图模型.
/// </summary>
public sealed partial class EpisodeItemViewModel : ViewModelBase<EpisodeInformation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeItemViewModel"/> class.
    /// </summary>
    public EpisodeItemViewModel(EpisodeInformation data, EpisodeCardStyle style)
        : base(data)
    {
        Title = data.Identifier.Title;
        Cover = data.Identifier.Cover?.Uri;
        PlayCount = data.CommunityInformation?.PlayCount;
        DanmakuCount = data.CommunityInformation?.DanmakuCount;
        CommentCount = data.CommunityInformation?.CommentCount;
        CoinCount = data.CommunityInformation?.CoinCount;
        LikeCount = data.CommunityInformation?.LikeCount;
        Style = style;
        Duration = AppToolkit.FormatDuration(TimeSpan.FromSeconds(data.Duration ?? 0));
        IsPreview = data.GetExtensionIfNotNull<bool>(EpisodeExtensionDataId.IsPreview);
        HighlightText = data.GetExtensionIfNotNull<string>(EpisodeExtensionDataId.RecommendReason);
        Index = data.Index;
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

        MediaIdentifier id = new("ep_" + Data.Identifier.Id, null, null);
        ItemServices.Navigation.NavigateToOver(typeof(PgcPlayerPage), id);
    }

    [RelayCommand]
    private Task OpenInBroswerAsync() => Launcher.LaunchUriAsync(new Uri(GetWebUrl())).AsTask();

    [RelayCommand]
    private void OpenInNewWindow() =>
        new PlayerWindow().OpenPgc(
            new MediaIdentifier("ep_" + Data.Identifier.Id, null, null)
        );

    [RelayCommand]
    private void Pin()
    {
        PinItem pinItem = new(
            $"ep_{Data.Identifier.Id}",
            Data.Identifier.Title,
            Data.Identifier.Cover.Uri.ToString(),
            PinContentType.Pgc
        );
        ItemServices.Pinner.AddItemCommand.Execute(pinItem);
    }

    private string GetWebUrl() => $"https://www.bilibili.com/bangumi/play/ep{Data.Identifier.Id}";
}
