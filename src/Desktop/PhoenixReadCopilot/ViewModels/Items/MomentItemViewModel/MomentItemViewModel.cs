using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.Core.Models.Media;
using PhoenixReadCopilot.Core.Models.Moment;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Core.Models.Player;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.Toolkits;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 动态条目视图模型.
/// </summary>
public sealed partial class MomentItemViewModel : ViewModelBase<MomentInformation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MomentItemViewModel"/> class.
    /// </summary>
    public MomentItemViewModel(
        MomentInformation data,
        MomentCardStyle style,
        Action<MomentItemViewModel> showCommentAction
    )
        : base(data)
    {
        _showMomentAction = showCommentAction;
        IsLiked = data.CommunityInformation?.IsLiked ?? false;
        LikeCount = data.CommunityInformation?.LikeCount;
        CommentCount = data.CommunityInformation?.CommentCount;
        Author = data.User?.Name;
        Avatar = data.User?.Avatar?.Uri;
        Tip = data.Tip;
        Description = data.Description;
        NoData = data.Data is null;
        Style = style;

        if (!NoData)
        {
            if (data.Data is MomentInformation forward)
            {
                InnerContent = new MomentItemViewModel(forward, Style, showCommentAction);
            }
            else if (data.Data is VideoInformation video)
            {
                InnerContent = new VideoItemViewModel(video, VideoCardStyle.Moment);
            }
            else if (data.Data is IEnumerable<AppImage> images)
            {
                InnerContent = images;
            }

            if (FindInnerContent<VideoInformation>() is VideoInformation vinfo)
            {
                VideoTitle = vinfo.Identifier.Title ?? Resources.NoTitleVideo;
                VideoCover = vinfo.Identifier.Cover?.Uri;
                VideoDuration = vinfo.Duration.HasValue
                    ? AppToolkit.FormatDuration(TimeSpan.FromSeconds(vinfo.Duration.Value))
                    : string.Empty;
            }
        }
    }

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        bool state = !IsLiked;
        try
        {
            // TODO: 红果动态点赞 API 待接入
            IsLiked = state;
            LikeCount += state ? 1 : -1;
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "切换评论点赞状态时失败");
        }
    }

    [RelayCommand]
    private void ShowComment() => _showMomentAction?.Invoke(this);

    [RelayCommand]
    private void Activate()
    {
        if (FindInnerContent<VideoInformation>() is VideoInformation vinfo)
        {
            if (!TryOpenInNewWindowIfPreferred())
            {
                VideoSnapshot snapshot = new(vinfo);
                ItemServices.Navigation.NavigateToOver(typeof(VideoPlayerPage), snapshot);
            }
        }

        bool TryOpenInNewWindowIfPreferred()
        {
            PlayerDisplayMode preferDisplayMode = ItemServices.Settings.DefaultPlayerDisplayMode;
            if (preferDisplayMode == PlayerDisplayMode.NewWindow)
            {
                OpenInNewWindowCommand.Execute(null);
                return true;
            }

            return false;
        }
    }

    [RelayCommand]
    private void ShowUserSpace() =>
        ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Data.User);

    [RelayCommand]
    private async Task AddToViewLaterAsync()
    {
        VideoInformation? vinfo = FindInnerContent<VideoInformation>();
        string aid = string.Empty;
        if (vinfo is null)
        {
            EpisodeInformation? einfo = FindInnerContent<EpisodeInformation>();
            if (einfo is null)
            {
                return;
            }

            aid = einfo.EpisodeId ?? einfo.Identifier.Id;
        }
        else
        {
            aid = vinfo.Identifier.Id;
        }

        string title = vinfo?.Identifier.Title ?? string.Empty;
        string cover = vinfo?.Identifier.Cover?.Uri?.ToString() ?? string.Empty;
        try
        {
            await ItemServices.LocalViewLater.AddAsync(new PhoenixReadApi.Drama
            {
                Id = aid,
                Title = title,
                CoverUrl = cover,
            });
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
    private void OpenInNewWindow()
    {
        if (FindInnerContent<VideoInformation>() is VideoInformation vinfo)
        {
            new PlayerWindow().OpenVideo(new VideoSnapshot(vinfo));
        }
    }

    [RelayCommand]
    private void Pin()
    {
        PinItem? pinItem = null;
        if (FindInnerContent<VideoInformation>() is VideoInformation vinfo)
        {
            pinItem = new PinItem(
                vinfo.Identifier.Id,
                vinfo.Identifier.Title,
                vinfo.Identifier.Cover?.Uri?.ToString() ?? string.Empty,
                PinContentType.Video
            );
        }

        if (pinItem is not null)
        {
            ItemServices.Pinner.AddItemCommand.Execute(pinItem);
        }
    }

    private T? FindInnerContent<T>()
        where T : class
    {
        if (Data.Data is T info)
        {
            return info;
        }
        else if (Data.Data is MomentInformation { Data: T info2 })
        {
            return info2;
        }

        return null;
    }
}
