using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.System;
using WinUIEx;
using WinUIKernel.Share.Base;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// PGC 播放器页面视图模型.
/// </summary>
public sealed partial class PgcPlayerPageViewModel
{
    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        try
        {
            bool state = !IsLiked;
            string aid = _episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Aid).ToString();
            await _service.ToggleVideoLikeAsync(aid, state);
            IsLiked = state;
            ReloadEpisodeOpeartionCommand.Execute(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试点赞/取消点赞视频时失败.");
        }
    }

    [RelayCommand]
    private async Task CoinAsync(int count = 1)
    {
        try
        {
            string aid = _episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Aid).ToString();
            await _service.CoinVideoAsync(aid, count, IsCoinAlsoLike);
            IsCoined = true;
            if (IsCoinAlsoLike && !IsLiked)
            {
                IsLiked = true;
            }

            ReloadEpisodeOpeartionCommand.Execute(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试投币/取消投币视频时失败.");
        }
    }

    [RelayCommand]
    private async Task TripleAsync()
    {
        try
        {
            string aid = _episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Aid).ToString();
            await _service.TripleVideoAsync(aid);
            IsLiked = true;
            IsCoined = true;
            IsFavorited = true;
            ReloadEpisodeOpeartionCommand.Execute(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试一键三连视频时失败.");
        }
    }

    [RelayCommand]
    private async Task InitializeFavoritesAsync()
    {
        if (FavoriteFolders is not null)
        {
            return;
        }

        try
        {
            string aid = _episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Aid).ToString();
            (IReadOnlyList<VideoFavoriteFolder>? folders, IReadOnlyList<string>? ids) =
                await _favoriteService.GetPlayingVideoFavoriteFoldersAsync(aid);
            FavoriteFolders =
            [
                .. folders
                    .Select(p => new PlayerFavoriteFolderViewModel(p, ids.Contains(p.Id))),
            ];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试获取收藏夹列表时失败.");
        }
    }

    [RelayCommand]
    private async Task FavoriteAsync()
    {
        if (FavoriteFolders is null)
        {
            return;
        }

        List<string>? selectedFolders = FavoriteFolders
            ?.Where(p => p.IsSelected)
            .Select(p => p.Data.Id)
            .ToList();
        List<string>? unselectedFolders = FavoriteFolders
            ?.Where(p => !p.IsSelected)
            .Select(p => p.Data.Id)
            .ToList();
        try
        {
            string aid = _episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Aid).ToString();
            await _service.FavoriteVideoAsync(aid, selectedFolders, unselectedFolders);
            IsFavorited = selectedFolders.Count != 0;
            FavoriteFolders = null;
            ReloadEpisodeOpeartionCommand.Execute(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试收藏视频时失败.");
        }
    }

    [RelayCommand]
    private void CopyEpisodeUrl()
    {
        string url = GetEpisodeUrl();
        DataPackage dp = new();
        dp.SetText(url);
        dp.SetWebLink(new Uri(url));
        Clipboard.SetContent(dp);
        _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
    }

    [RelayCommand]
    private void CopySeasonUrl()
    {
        string url = GetSeasonUrl();
        DataPackage dp = new();
        dp.SetText(url);
        dp.SetWebLink(new Uri(url));
        Clipboard.SetContent(dp);
        _appViewModel.ShowTipCommand.Execute((Resources.Copied, InfoType.Success));
    }

    [RelayCommand]
    private void ShareEpisodeUrl()
    {
        nint handle = _appViewModel.ActivatedWindow.GetWindowHandle();
        DataTransferManager transferManager = DataTransferManagerInterop.GetForWindow(handle);
        transferManager.DataRequested += OnTransferDataRequested;
        DataTransferManagerInterop.ShowShareUIForWindow(handle);

        void OnTransferDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            string url = GetEpisodeUrl();
            DataPackage dp = new();
            dp.SetText(url);
            dp.SetWebLink(new Uri(url));
            dp.Properties.Title = EpisodeTitle;
            dp.Properties.Description = Description;
            dp.Properties.Thumbnail = RandomAccessStreamReference.CreateFromUri(
                _view.Information.Identifier.Cover.Uri
            );
            args.Request.Data = dp;
            sender.DataRequested -= OnTransferDataRequested;
        }
    }

    [RelayCommand]
    private async Task OpenInBroswerAsync()
    {
        string url = GetEpisodeUrl();
        await Launcher.LaunchUriAsync(new Uri(url)).AsTask();
    }

    [RelayCommand]
    private void OpenInNewWindow()
    {
        if (Player.Position > 0)
        {
            ReportProgressCommand.Execute(Player.Position);
        }

        if (!Player.IsPaused)
        {
            Player.TogglePlayPauseCommand.Execute(null);
        }

        MediaIdentifier identifier = new(
            "ep_" + _episode.Identifier.Id,
            _episode.Identifier.Title,
            _episode.Identifier.Cover
        );
        new PlayerWindow().OpenPgc(identifier);
    }

    [RelayCommand]
    private void PlayNextEpisode()
    {
        if (IsPageLoading || Player.IsPlayerDataLoading)
        {
            return;
        }

        object? nextPart = FindNextEpisode();
        if (nextPart is null)
        {
            Player.BackToDefaultModeCommand.Execute(null);
            return;
        }

        if (nextPart is EpisodeInformation part)
        {
            ChangeEpisode(part);
        }
        else if (nextPart is VideoInformation video)
        {
            _navigationViewModel.NavigateToOver(typeof(VideoPlayerPage), new VideoSnapshot(video));
        }
        else
        {
            Player.BackToDefaultModeCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task ReloadEpisodeOpeartionAsync()
    {
        if (_episode is null)
        {
            return;
        }

        try
        {
            VideoOperationInformation operation =
                await _service.GetEpisodeOperationInformationAsync(_episode.Identifier.Id);
            IsLiked = operation.IsLiked;
            IsCoined = operation.IsCoined;
            IsFavorited = operation.IsFavorited;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试获取剧集操作信息时失败.");
        }
    }

    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        try
        {
            bool state = !IsFollow;
            if (state)
            {
                await _discoveryService.FollowAsync(_view.Information.Identifier.Id);
            }
            else
            {
                await _discoveryService.UnfollowAsync(_view.Information.Identifier.Id);
            }

            IsFollow = state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"尝试追番/追剧失败 ({!IsFollow})");
        }
    }

    private void ChangeEpisode(EpisodeInformation episode)
    {
        if (_initialProgress == 0)
        {
            _initialProgress = -1;
        }

        bool isFirstSet = _episode is null;
        _episode = episode;
        if (isFirstSet)
        {
            LoadInitialProgress();
        }

        if (_injectedProgress is not null)
        {
            _initialProgress = _injectedProgress.Value;
            _injectedProgress = null;
        }

        Player.CancelNotification();
        EpisodeId = episode.Identifier.Id;
        EpisodeTitle = SeasonTitle + " - " + episode.Identifier.Title;
        Player.Title = EpisodeTitle;
        string aid = episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Aid).ToString();
        string cid = episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Cid).ToString();
        _comments.Initialize(aid, CommentTargetType.Video, CommentSortType.Hot);
        Subtitle?.ResetData(aid, cid);
        CalcPlayerHeight();
        ReloadEpisodeOpeartionCommand.Execute(null);
        InitializeDashMediaCommand.Execute(episode);
        Subtitle.InitializeCommand.Execute(null);
        InitializeNextEpisode();
    }

    [RelayCommand]
    private void Pin()
    {
        PinItem pinItem = new(
            $"ss_{_view.Information.Identifier.Id}",
            _view.Information.Identifier.Title,
            _view.Information.Identifier.Cover.Uri.ToString(),
            PinContentType.Pgc
        );
        _pinnerViewModel.AddItemCommand.Execute(pinItem);
    }

    private void PlayerProgressChanged(int progress, int duration)
    {
        if (progress < duration && progress > 1 && (Danmaku?.IsEmpty() ?? false))
        {
            string aid = _episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Aid).ToString();
            string cid = _episode.GetExtensionIfNotNull<long>(EpisodeExtensionDataId.Cid).ToString();
            Danmaku.ResetData(aid, cid);
            Danmaku.LoadDanmakusCommand.Execute(duration);
        }

        Danmaku?.UpdatePosition(progress);
        Subtitle?.UpdatePosition(progress);
    }

    private void PlayerStateChanged(PlayerState state)
    {
        if (_view is null)
        {
            return;
        }

        if (state == PlayerState.Playing)
        {
            Danmaku?.Resume();
        }
        else
        {
            if (state == PlayerState.Paused)
            {
                // 记录播放进度.
                ReportProgressCommand.Execute(Player.Position);
            }

            Danmaku?.Pause();
        }
    }

    private void PlayerSpeedChanged(double speed)
    {
        Danmaku?.ExtraSpeed = speed;
    }

    private void PlayerMediaEnded()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            // 清除弹幕.
            Danmaku.ClearDanmaku();
            Subtitle.ClearSubtitle();

            ReportProgressCommand.Execute(Player.Duration);

            bool autoNext = _settings.AutoPlayNext;
            if ((!autoNext || !HasNextEpisode) && !_isFormatChanging)
            {
                Player.BackToDefaultModeCommand.Execute(null);
                return;
            }

            if (IsPageLoading || Player.IsPlayerDataLoading)
            {
                return;
            }

            object? next = FindNextEpisode();
            if (next is null)
            {
                return;
            }

            PlayNextEpisode();
        });
    }

    private string GetSeasonUrl() =>
        $"https://www.bilibili.com/bangumi/play/ss{_view.Information.Identifier.Id}";

    private string GetEpisodeUrl() =>
        $"https://www.bilibili.com/bangumi/play/ep{_episode.Identifier.Id}";
}
