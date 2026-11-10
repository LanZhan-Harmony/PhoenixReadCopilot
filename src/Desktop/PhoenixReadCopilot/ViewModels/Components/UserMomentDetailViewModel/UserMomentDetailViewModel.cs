using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Models.Appearance;
using Richasy.BiliKernel.Models.Media;
using Richasy.BiliKernel.Models.Moment;
using Richasy.BiliKernel.Models.User;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 用户动态/作品详情视图模型.
/// </summary>
public sealed partial class UserMomentDetailViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserMomentDetailViewModel"/> class.
    /// </summary>
    public UserMomentDetailViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<UserMomentDetailViewModel> logger
    )
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// 设置是否为视频动态.
    /// </summary>
    public void SetIsVideo(bool isVideo)
    {
        _isVideo = isVideo;
        Title = isVideo ? Resources.Video : Resources.Comprehensive;
    }

    /// <summary>
    /// 设置显示评论的动作.
    /// </summary>
    public void SetShowCommentAction(Action<MomentItemViewModel> action) =>
        _showCommentAction = action;

    /// <summary>
    /// 是否为视频动态模块.
    /// </summary>
    /// <returns>结果.</returns>
    public bool IsVideo() => _isVideo;

    /// <summary>
    /// 设置初始短剧列表.
    /// </summary>
    public void SetInitialData(IReadOnlyList<Drama>? dramas, string? cursor, bool hasMore)
    {
        Clear();
        if (dramas is { Count: > 0 })
        {
            foreach (Drama drama in dramas)
            {
                MomentInformation momentInfo = CreateDramaMoment(drama);
                Items.Add(new MomentItemViewModel(momentInfo, MomentCardStyle.Video, _showCommentAction!));
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
            _offset = cursor;
            _preventLoadMore = !hasMore;
            IsEmpty = false;
        }
        else
        {
            _offset = cursor;
            _preventLoadMore = false;
            IsEmpty = true;
        }
    }

    /// <summary>
    /// 设置初始动态列表.
    /// </summary>
    public void SetInitialData(IReadOnlyList<UserActivity>? activities, string? cursor, bool hasMore)
    {
        Clear();
        if (activities is { Count: > 0 })
        {
            foreach (UserActivity activity in activities)
            {
                MomentInformation momentInfo = CreateActivityMoment(activity);
                Items.Add(new MomentItemViewModel(momentInfo, MomentCardStyle.Comprehensive, _showCommentAction!));
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
            _offset = cursor;
            _preventLoadMore = !hasMore;
            IsEmpty = false;
        }
        else
        {
            _offset = cursor;
            _preventLoadMore = false;
            IsEmpty = true;
        }
    }

    [RelayCommand]
    private void Reset(object userParam)
    {
        Clear();
        if (userParam is PhoenixReadApi.UserProfile profile)
        {
            _userId = profile.User.Id;
            if (_isVideo)
            {
                SetInitialData(profile.RelatedDramas, profile.RelatedDramasPage.Cursor, profile.RelatedDramasPage.HasMore);
            }
            else
            {
                SetInitialData(profile.Activities, profile.ActivitiesPage.Cursor, profile.ActivitiesPage.HasMore);
            }
        }
        else if (userParam is string id)
        {
            _userId = id;
        }
        else if (userParam is ActorLeaderboardItem actor)
        {
            _userId = !string.IsNullOrEmpty(actor.UserId) ? actor.UserId : actor.ActorId;
        }
        else if (userParam is Richasy.BiliKernel.Models.User.UserProfile bp)
        {
            _userId = bp.Id;
        }
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Items.Count > 0 || _preventLoadMore)
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Clear();
        await LoadItemsAsync();
    }

    [RelayCommand]
    private void Clear()
    {
        _offset = null;
        IsEmpty = false;
        _preventLoadMore = false;
        Items.Clear();
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading || _preventLoadMore || string.IsNullOrEmpty(_userId))
        {
            return;
        }

        try
        {
            IsLoading = true;
            if (_isVideo)
            {
                UserDramaPage page = await _apiClient.GetUserRelatedDramasAsync(
                    _userId,
                    count: 18,
                    cursor: _offset ?? string.Empty
                );
                _offset = page.Cursor;
                _preventLoadMore = !page.HasMore || string.IsNullOrEmpty(_offset);
                if (page.Items.Count > 0)
                {
                    foreach (Drama drama in page.Items)
                    {
                        MomentInformation momentInfo = CreateDramaMoment(drama);
                        Items.Add(new MomentItemViewModel(momentInfo, MomentCardStyle.Video, _showCommentAction!));
                    }
                    ListUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                UserActivityPage page = await _apiClient.GetUserActivitiesAsync(
                    _userId,
                    count: 18,
                    cursor: _offset ?? string.Empty
                );
                _offset = page.Cursor;
                _preventLoadMore = !page.HasMore || string.IsNullOrEmpty(_offset);
                if (page.Items.Count > 0)
                {
                    foreach (UserActivity activity in page.Items)
                    {
                        MomentInformation momentInfo = CreateActivityMoment(activity);
                        Items.Add(new MomentItemViewModel(momentInfo, MomentCardStyle.Comprehensive, _showCommentAction!));
                    }
                    ListUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        catch (Exception ex)
        {
            _preventLoadMore = true;
            _logger.LogError(ex, "加载用户动态/作品失败.");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
            IsLoading = false;
        }
    }

    private static MomentInformation CreateDramaMoment(Drama drama)
    {
        BiliImage? cover = !string.IsNullOrEmpty(drama.CoverUrl) && Uri.TryCreate(drama.CoverUrl, UriKind.Absolute, out Uri? coverUri)
            ? new BiliImage(coverUri)
            : null;

        VideoInformation video = new(
            new MediaIdentifier(drama.Id, drama.Title, cover),
            null
        );

        return new MomentInformation(
            id: drama.Id,
            user: new Richasy.BiliKernel.Models.User.UserProfile(drama.Id, drama.Category ?? "短剧", null),
            communityInformation: null,
            tip: !string.IsNullOrEmpty(drama.Score) ? $"评分: {drama.Score}" : null,
            description: new EmoteText(drama.Remark ?? drama.Title, null),
            data: video
        );
    }

    private static MomentInformation CreateActivityMoment(UserActivity activity)
    {
        return new MomentInformation(
            id: activity.Id,
            user: null,
            communityInformation: null,
            tip: null,
            description: new EmoteText(activity.Text, null),
            data: null
        );
    }
}
