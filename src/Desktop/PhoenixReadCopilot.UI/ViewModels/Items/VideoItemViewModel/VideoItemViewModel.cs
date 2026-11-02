using System.Globalization;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Forms;
using PhoenixReadCopilot.UI.Models;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages.Overlay;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.Toolkits;
using Richasy.BiliKernel.Models.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Globalization;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Items;

/// <summary>
/// 视频项视图模型.
/// </summary>
public sealed partial class VideoItemViewModel : ViewModelBase<VideoInformation>
{
    private static ILogger<VideoItemViewModel>? _logger;
    private static ILogger<VideoItemViewModel> Logger =>
        _logger ??= Ioc.Default.GetRequiredService<ILogger<VideoItemViewModel>>();

    private readonly Action<VideoItemViewModel>? _removeAction;
    private readonly VideoFavoriteFolder? _favFolder;

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
            ItemServices.Navigation.NavigateToOver(typeof(WebPlayerPage), GetWebUri().ToString());
            return;
        }

        ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), new VideoSnapshot(Data));
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
            Logger.LogError(ex, "添加稍后再看失败");
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
            Logger.LogError(ex, "移除稍后再看视频失败");
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
            Logger.LogError(ex, "移除历史记录视频失败");
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
            Logger.LogError(ex, "移除收藏视频失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Resources.FailedToRemoveVideoFromFavorite, InfoType.Error)
            );
        }
    }

    [RelayCommand]
    private async Task OpenInBroswerAsync() =>
        await Windows.System.Launcher.LaunchUriAsync(GetWebUri()).AsTask();

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
        var pinItem = new PinItem(
            Data.Identifier.Id,
            Data.Identifier.Title,
            Data.Identifier.Cover.Uri.ToString(),
            PinContentType.Video
        );
        ItemServices.Pinner.AddItemCommand.Execute(pinItem);
    }

    private Uri GetWebUri()
    {
        var shortLink = Data.GetExtensionIfNotNull<string>(VideoExtensionDataId.ShortLink);
        return string.IsNullOrEmpty(shortLink)
            ? new Uri($"https://www.bilibili.com/video/av{Data.Identifier.Id}")
            : new Uri(shortLink);
    }
}
