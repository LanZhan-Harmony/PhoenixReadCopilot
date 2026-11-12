using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Moment;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 用户空间/主页视图模型.
/// </summary>
public sealed partial class UserSpacePageViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSpacePageViewModel"/> class.
    /// </summary>
    public UserSpacePageViewModel(
        PhoenixReadApiClient apiClient,
        CommentMainViewModel comment,
        ILogger<UserSpacePageViewModel> logger,
        AppViewModel appViewModel,
        PinnerViewModel pinnerViewModel,
        ISettingsService settingsService,
        DispatcherQueue dispatcherQueue,
        ILoggerFactory loggerFactory
    )
    {
        _apiClient = apiClient;
        CommentModule = comment;
        _logger = logger;
        _appViewModel = appViewModel;
        _pinnerViewModel = pinnerViewModel;
        _settings = settingsService.Settings;
        _dispatcherQueue = dispatcherQueue;
        _loggerFactory = loggerFactory;
    }

    [RelayCommand]
    private async Task InitializeAsync(object parameter)
    {
        if (IsSearchMode)
        {
            ExitSearch();
        }

        try
        {
            UserProfile? profile = null;
            string fallbackAvatarUrl = string.Empty;
            if (parameter is DramaCredit credit)
            {
                _userId = credit.User?.Id ?? string.Empty;
                UserName = credit.User?.Name ?? string.Empty;
                fallbackAvatarUrl = credit.User?.AvatarUrl ?? string.Empty;
                profile = await _apiClient.GetUserProfileAsync(credit);
            }
            else if (parameter is ActorLeaderboardItem actor)
            {
                _userId = !string.IsNullOrEmpty(actor.UserId) ? actor.UserId : actor.ActorId;
                UserName = actor.Name;
                fallbackAvatarUrl = actor.AvatarUrl ?? string.Empty;
                profile = await _apiClient.GetUserProfileAsync(_userId);
            }
            else if (parameter is string id)
            {
                _userId = id;
                profile = await _apiClient.GetUserProfileAsync(id);
            }
            else if (parameter is PhoenixReadCopilot.Core.Models.User.User user)
            {
                _userId = user.Id;
                UserName = user.Name;
                fallbackAvatarUrl = user.Avatar?.Uri?.ToString() ?? string.Empty;
                profile = await _apiClient.GetUserProfileAsync(user.Id);
            }
            else if (parameter is PhoenixReadApi.User apiUser)
            {
                _userId = apiUser.Id;
                UserName = apiUser.Name;
                fallbackAvatarUrl = apiUser.AvatarUrl ?? string.Empty;
                profile = await _apiClient.GetUserProfileAsync(apiUser.Id);
            }
            else if (parameter is UserProfile up)
            {
                _userId = up.User.Id;
                UserName = up.User.Name;
                fallbackAvatarUrl = up.User.AvatarUrl ?? string.Empty;
                profile = up;
            }

            if (profile is not null)
            {
                _phoenixProfile = profile;
                UserName = !string.IsNullOrEmpty(profile.User.Name) ? profile.User.Name : UserName;
                IsFollowed = false;
                Card = CreateUserCard(profile, UserName, fallbackAvatarUrl);
            }
            else if (!string.IsNullOrEmpty(UserName))
            {
                UserAvatar? avatar =
                    !string.IsNullOrEmpty(fallbackAvatarUrl)
                    && Uri.TryCreate(fallbackAvatarUrl, UriKind.Absolute, out Uri? uri)
                        ? new UserAvatar { Uri = uri }
                        : null;

                Card = new UserCard
                {
                    Profile = new UserDetailProfile
                    {
                        User = new PhoenixReadCopilot.Core.Models.User.User
                        {
                            Id = _userId,
                            Name = UserName,
                            Avatar = avatar,
                        },
                    },
                    Community = new UserCommunity(),
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取红果创作者主页失败: {UserId}", _userId);
        }

        if (Sections is null)
        {
            Sections =
            [
                new(_apiClient, _loggerFactory.CreateLogger<UserMomentDetailViewModel>()),
                new(_apiClient, _loggerFactory.CreateLogger<UserMomentDetailViewModel>()),
            ];

            Sections.ElementAt(0).SetIsVideo(true);
            Sections.ElementAt(1).SetIsVideo(false);
        }

        foreach (UserMomentDetailViewModel item in Sections)
        {
            item.SetShowCommentAction(ShowComment);
            item.ResetCommand.Execute((object?)_phoenixProfile ?? _userId);
        }

        if (SelectedSection is null)
        {
            bool isLastVideoSection = _settings.LastUserSpaceSectionIsVideo;
            UserMomentDetailViewModel section = isLastVideoSection
                ? Sections.First()
                : Sections.Last();
            SelectSection(section);
        }
        else
        {
            SelectedSection.InitializeCommand.Execute(null);
        }

        Initialized?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Search(string text)
    {
        ExitSearch();
        SearchKeyword = text;
        IsSearchMode = true;
        LoadMoreSearchResultCommand.Execute(null);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsSearchMode)
        {
            string keyword = SearchKeyword;
            SearchCommand.Execute(keyword);
            return;
        }

        if (SelectedSection is not null)
        {
            await SelectedSection.RefreshCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private void SelectSection(UserMomentDetailViewModel section)
    {
        if (section is null || section == SelectedSection)
        {
            return;
        }

        SelectedSection = section;
        _settings.LastUserSpaceSectionIsVideo = SelectedSection.IsVideo();
        SelectedSection.InitializeCommand.Execute(null);
    }

    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        if (string.IsNullOrEmpty(_userId))
        {
            return;
        }

        if (!_apiClient.GetAuthenticationState().IsAuthenticated)
        {
            _appViewModel.ShowTipCommand.Execute(("请先登录后再关注", InfoType.Warning));
            return;
        }

        try
        {
            bool next = !IsFollowed;
            PhoenixOperationResult res = await _apiClient.SetFollowAsync(_userId, next);
            if (res.Succeeded)
            {
                IsFollowed = next;
                _appViewModel.ShowTipCommand.Execute(
                    (next ? Resources.Followed : Resources.Unfollowed, InfoType.Success)
                );
            }
            else
            {
                _appViewModel.ShowTipCommand.Execute(
                    (res.Message ?? "关注操作失败", InfoType.Error)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换关注状态时失败");
            _appViewModel.ShowTipCommand.Execute((Resources.FailedToFollowUser, InfoType.Error));
        }
    }

    [RelayCommand]
    private void Pin()
    {
        string avatarUri = Card?.Profile?.User?.Avatar?.Uri?.ToString() ?? string.Empty;
        PinItem pinItem = new(_userId, UserName, avatarUri, PinContentType.User);
        _pinnerViewModel.AddItemCommand.Execute(pinItem);
    }

    [RelayCommand]
    private async Task InitializeUserInformationAsync()
    {
        if (Card is not null || string.IsNullOrEmpty(_userId))
        {
            return;
        }

        try
        {
            UserProfile profile = await _apiClient.GetUserProfileAsync(_userId);
            Card = CreateUserCard(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取创作者信息失败");
        }
    }

    [RelayCommand]
    private async Task LoadMoreSearchResultAsync()
    {
        if (
            _preventLoadMoreSearch
            || IsSearching
            || !IsSearchMode
            || string.IsNullOrEmpty(SearchKeyword)
            || string.IsNullOrEmpty(_userId)
        )
        {
            return;
        }

        try
        {
            IsSearching = true;
            _searchCancellationTokenSource = new CancellationTokenSource();
            CancellationToken ct = _searchCancellationTokenSource.Token;

            UserDramaPage dramas = await _apiClient.GetUserRelatedDramasAsync(
                _userId,
                count: 18,
                cursor: _searchOffset.ToString(),
                cancellationToken: ct
            );

            _preventLoadMoreSearch = !dramas.HasMore || dramas.Items.Count == 0;
            if (int.TryParse(dramas.Cursor, out int nextCursor))
            {
                _searchOffset = nextCursor;
            }

            string kw = SearchKeyword.Trim();
            List<Drama> matched =
            [
                .. dramas.Items.Where(d =>
                    d.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)
                ),
            ];

            foreach (Drama item in matched)
            {
                SearchVideos.Add(new VideoItemViewModel(item, VideoCardStyle.Search));
            }

            SearchUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (TaskCanceledException)
        {
            // Do nothing.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索用户短剧作品时失败");
        }
        finally
        {
            IsSearchEmpty = SearchVideos.Count == 0;
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void ExitSearch()
    {
        IsSearchMode = false;
        SearchKeyword = string.Empty;
        IsSearchEmpty = false;
        SearchVideos.Clear();
        _preventLoadMoreSearch = false;
        _searchOffset = 0;
        CancelSearch();
    }

    private static UserCard CreateUserCard(UserProfile profile, string fallbackName = "", string fallbackAvatarUrl = "")
    {
        string name = !string.IsNullOrEmpty(profile.User.Name)
            ? profile.User.Name
            : (!string.IsNullOrEmpty(fallbackName) ? fallbackName : string.Empty);
        string avatarUrl = !string.IsNullOrEmpty(profile.User.AvatarUrl)
            ? profile.User.AvatarUrl
            : fallbackAvatarUrl;
        UserAvatar? avatar =
            !string.IsNullOrEmpty(avatarUrl)
            && Uri.TryCreate(avatarUrl, UriKind.Absolute, out Uri? uri)
                ? new UserAvatar { Uri = uri }
                : null;

        return new UserCard
        {
            Profile = new UserDetailProfile
            {
                User = new PhoenixReadCopilot.Core.Models.User.User
                {
                    Id = !string.IsNullOrEmpty(profile.User.Id) ? profile.User.Id : string.Empty,
                    Name = name,
                    Avatar = avatar,
                },
                Introduce = profile.User.Introduction,
            },
            Community = new UserCommunity
            {
                FollowCount = profile.CollectionsPage.Total,
                FansCount = profile.LikesPage.Total,
                LikeCount = profile.RelatedDramasPage.Total,
            },
        };
    }

    private void ShowComment(MomentItemViewModel data)
    {
        MomentInformation moment = data.Data;
        if (CommentModule.Id == moment.CommentId)
        {
            return;
        }

        IsCommentsOpened = true;
        CommentModule.Initialize(moment.CommentId);
        CommentModule.RefreshCommand.Execute(null);
    }

    private void CancelSearch()
    {
        if (_searchCancellationTokenSource is not null)
        {
            _searchCancellationTokenSource.Cancel();
            _searchCancellationTokenSource.Dispose();
            _searchCancellationTokenSource = null;
        }
    }
}
