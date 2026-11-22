using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 红果圈图文详情视图模型（模仿小红书左图右文评交互）.
/// </summary>
public sealed partial class ArticlePageViewModel : ViewModelBase
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IFollowService _followService;
    private readonly INavigationService _navigationService;
    private readonly ILogger<ArticlePageViewModel> _logger;
    private readonly AppViewModel _appViewModel;
    private readonly DispatcherQueue _dispatcherQueue;

    private string _postId = string.Empty;

    [ObservableProperty]
    public partial CommentMainViewModel CommentMainViewModel { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial CirclePost? Post { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<CircleImage> Images { get; set; }

    [ObservableProperty]
    public partial int SelectedImageIndex { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Content { get; set; }

    [ObservableProperty]
    public partial string AuthorName { get; set; }

    [ObservableProperty]
    public partial string AuthorAvatarUrl { get; set; }

    [ObservableProperty]
    public partial string AuthorId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FollowText))]
    public partial bool IsFollowed { get; set; }

    public string FollowText => IsFollowed ? Resources.Followed : Resources.Follow;

    [ObservableProperty]
    public partial bool IsLiked { get; set; }

    [ObservableProperty]
    public partial long LikeCount { get; set; }

    [ObservableProperty]
    public partial long CommentCount { get; set; }

    [ObservableProperty]
    public partial string PublishTime { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Comment> Comments { get; set; }

    [ObservableProperty]
    public partial string CommentInputText { get; set; }

    [ObservableProperty]
    public partial bool IsEmptyComments { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Drama> RelatedDramas { get; set; }

    public ArticlePageViewModel(
        PhoenixReadApiClient apiClient,
        IFollowService followService,
        INavigationService navigationService,
        ILogger<ArticlePageViewModel> logger,
        AppViewModel appViewModel,
        DispatcherQueue dispatcherQueue,
        CommentMainViewModel commentMainViewModel
    )
    {
        _apiClient = apiClient;
        _followService = followService;
        _navigationService = navigationService;
        _logger = logger;
        _appViewModel = appViewModel;
        _dispatcherQueue = dispatcherQueue;
        CommentMainViewModel = commentMainViewModel;

        Images = [];
        Comments = [];
        RelatedDramas = [];
        Title = string.Empty;
        Content = string.Empty;
        AuthorName = string.Empty;
        AuthorAvatarUrl = string.Empty;
        AuthorId = string.Empty;
        PublishTime = string.Empty;
        CommentInputText = string.Empty;
    }

    public async Task InitializeAsync(object parameter)
    {
        string postId = string.Empty;
        if (parameter is CircleItem circleItem)
        {
            postId = circleItem.Id;
            Title = circleItem.Title;
            Content = circleItem.Text;
            AuthorName = circleItem.Author.Name;
            AuthorAvatarUrl = circleItem.Author.AvatarUrl;
            AuthorId = circleItem.Author.Id;
            LikeCount = circleItem.LikeCount;
            CommentCount = circleItem.CommentCount;
            if (!string.IsNullOrEmpty(circleItem.CoverUrl))
            {
                Images.Clear();
                Images.Add(
                    new CircleImage
                    {
                        Url = circleItem.CoverUrl,
                        Width = circleItem.CoverWidth,
                        Height = circleItem.CoverHeight,
                    }
                );
            }
        }
        else if (parameter is CirclePost post)
        {
            postId = post.Id;
            ApplyPost(post);
        }
        else if (parameter is string id)
        {
            postId = id;
        }

        if (string.IsNullOrEmpty(postId))
        {
            return;
        }

        _postId = postId;
        CommentMainViewModel.InitializeForCirclePost(postId);
        _ = CommentMainViewModel.TryFirstLoadAsync();

        IsLoading = true;
        try
        {
            CirclePost detailedPost = await _apiClient.GetCirclePostAsync(postId);
            ApplyPost(detailedPost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取图文详情失败: {PostId}", postId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyPost(CirclePost post)
    {
        Post = post;
        Title = post.Title;
        Content = post.PureText;
        AuthorName = post.Author.Name;
        AuthorAvatarUrl = post.Author.AvatarUrl;
        AuthorId = post.Author.Id;
        LikeCount = post.LikeCount;
        CommentCount = post.CommentCount;
        IsLiked = post.IsLiked;
        IsFollowed = _followService.IsFollowing(post.Author.Id);

        if (post.CreatedAtUnixSeconds > 0)
        {
            DateTimeOffset time = DateTimeOffset
                .FromUnixTimeSeconds(post.CreatedAtUnixSeconds)
                .ToLocalTime();
            PublishTime = time.ToString("yyyy-MM-dd HH:mm");
        }

        Images.Clear();
        foreach (CircleImage img in post.Images)
        {
            Images.Add(img);
        }

        RelatedDramas.Clear();
        foreach (Drama drama in post.RelatedDramas)
        {
            RelatedDramas.Add(drama);
        }
    }

    public Task LoadCommentsAsync(bool reset = false)
    {
        return reset ? CommentMainViewModel.RefreshAsync() : CommentMainViewModel.LoadItemsAsync();
    }

    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        if (string.IsNullOrEmpty(AuthorId))
        {
            return;
        }

        try
        {
            bool next = !IsFollowed;
            PhoenixReadApi.User? user = Post?.Author;
            PhoenixOperationResult res = await _followService.SetFollowAsync(AuthorId, next, user);
            if (res.Succeeded)
            {
                IsFollowed = next;
                _appViewModel.ShowTip(
                    next ? Resources.Followed : Resources.Unfollowed,
                    InfoType.Success
                );
            }
            else
            {
                _appViewModel.ShowTip(res.Message ?? "关注操作失败", InfoType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关注操作异常");
            _appViewModel.ShowTip(Resources.FailedToFollowUser, InfoType.Error);
        }
    }

    [RelayCommand]
    private void PlayDrama(string dramaId)
    {
        if (!string.IsNullOrEmpty(dramaId))
        {
            _navigationService.NavigateToOver(typeof(VideoPlayerPage), dramaId);
        }
    }
}
