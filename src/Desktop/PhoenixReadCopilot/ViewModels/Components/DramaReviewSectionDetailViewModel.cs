using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 剧评列表项视图模型.
/// </summary>
public sealed partial class DramaReviewItemViewModel : ViewModelBase<DramaComment>
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly AppViewModel _appViewModel;
    private readonly ILogger _logger;
    private readonly string _seriesId;
    private readonly Action<DramaReviewItemViewModel>? _showMoreAction;

    public DramaReviewItemViewModel(
        DramaComment comment,
        string seriesId,
        PhoenixReadApiClient apiClient,
        AppViewModel appViewModel,
        ILogger logger,
        Action<DramaReviewItemViewModel>? showMoreAction = null
    )
        : base(comment)
    {
        _seriesId = seriesId;
        _apiClient = apiClient;
        _appViewModel = appViewModel;
        _logger = logger;
        _showMoreAction = showMoreAction;

        Id = comment.Id;
        Text = comment.Text;
        UserName = comment.Author?.Name ?? "匿名观众";
        if (
            !string.IsNullOrEmpty(comment.Author?.AvatarUrl)
            && Uri.TryCreate(comment.Author.AvatarUrl, UriKind.Absolute, out Uri? avatarUri)
        )
        {
            Avatar = avatarUri;
        }

        LikeCount = (int)comment.LikeCount;
        ReplyCount = (int)comment.ReplyCount;

        if (comment.CreatedAtUnixSeconds > 0)
        {
            DateTimeOffset time = DateTimeOffset
                .FromUnixTimeSeconds(comment.CreatedAtUnixSeconds)
                .ToLocalTime();
            RelativeTime = time.Humanize();
            ActualTime = time.ToString("yyyy-MM-dd HH:mm:ss");
        }
        else
        {
            RelativeTime = "刚刚";
            ActualTime = string.Empty;
        }

        List<AppImage>? pictures =
            comment.Images?.Count > 0
                ?
                [
                    .. comment
                        .Images.Where(img =>
                            !string.IsNullOrEmpty(img.Url)
                            && Uri.TryCreate(img.Url, UriKind.Absolute, out _)
                        )
                        .Select(img => new AppImage(img)),
                ]
                : null;
        Content = new EmoteText(comment.Text, pictures);
    }

    public string Id { get; }
    public string Text { get; }
    public EmoteText Content { get; }
    public string UserName { get; }
    public Uri? Avatar { get; }
    public string RelativeTime { get; }
    public string ActualTime { get; }
    public int ReplyCount { get; }

    [ObservableProperty]
    public partial bool IsLiked { get; set; }

    [ObservableProperty]
    public partial int LikeCount { get; set; }

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        if (!_apiClient.GetAuthenticationState().IsAuthenticated)
        {
            _appViewModel.ShowTipCommand.Execute(("请先登录后再点赞", InfoType.Warning));
            return;
        }

        try
        {
            bool next = !IsLiked;
            PhoenixOperationResult res = await _apiClient.SetReviewLikeAsync(Id, _seriesId, next);
            if (res.Succeeded)
            {
                IsLiked = next;
                LikeCount += next ? 1 : -1;
            }
            else
            {
                _appViewModel.ShowTipCommand.Execute((res.Message ?? "点赞失败", InfoType.Error));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "点赞剧评失败.");
        }
    }

    [RelayCommand]
    private void ShowMore() => _showMoreAction?.Invoke(this);
}

/// <summary>
/// 剧评区块视图模型 (整部剧只有一份).
/// </summary>
public sealed partial class DramaReviewSectionDetailViewModel
    : ViewModelBase,
        IPlayerSectionDetailViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly AppViewModel _appViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<DramaReviewSectionDetailViewModel> _logger;

    private string _cursor = string.Empty;
    private bool _hasMore = true;

    public DramaReviewSectionDetailViewModel(
        PhoenixReadApiClient apiClient,
        AppViewModel appViewModel,
        DispatcherQueue dispatcherQueue,
        ILogger<DramaReviewSectionDetailViewModel> logger
    )
    {
        _apiClient = apiClient;
        _appViewModel = appViewModel;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;

        Reviews = [];
        Sorts = [CommentSortOrder.Hot, CommentSortOrder.Latest];
        SortType = CommentSortOrder.Hot;
        Rating = 5.0; // 默认 5 星满分
    }

    /// <inheritdoc/>
    public string Title => "剧评";

    [ObservableProperty]
    public partial string SeriesId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DramaScore { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double Rating { get; set; }

    [ObservableProperty]
    public partial bool IsLoggedIn { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSending { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial long TotalCount { get; set; }

    [ObservableProperty]
    public partial List<CommentSortOrder> Sorts { get; set; }

    [ObservableProperty]
    public partial CommentSortOrder SortType { get; set; }

    [ObservableProperty]
    public partial CommentDetailViewModel? SelectedItem { get; set; }

    /// <summary>
    /// 是否有更多剧评可加载.
    /// </summary>
    public bool HasMore => _hasMore;

    /// <summary>
    /// 列表已更新事件.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <summary>
    /// 初始化事件.
    /// </summary>
    public event EventHandler? Initialized;

    public ObservableCollection<DramaReviewItemViewModel> Reviews { get; }

    public bool CanSendReview => IsLoggedIn && !IsSending;

    public string ReviewPlaceholderText =>
        IsLoggedIn ? "发表你对本剧的评价..." : "请先登录后发表剧评";

    /// <summary>
    /// 初始化剧评组件.
    /// </summary>
    public void Initialize(string seriesId, string score)
    {
        SeriesId = seriesId;
        DramaScore = score;
        UpdateLoginState();
        Clear();
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateLoginState()
    {
        IsLoggedIn = _apiClient.GetAuthenticationState().IsAuthenticated;
        OnPropertyChanged(nameof(CanSendReview));
        OnPropertyChanged(nameof(ReviewPlaceholderText));
    }

    [RelayCommand]
    public async Task TryFirstLoadAsync()
    {
        if (Reviews.Count > 0 || string.IsNullOrEmpty(SeriesId))
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        Clear();
        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task ChangeSortTypeAsync(CommentSortOrder type)
    {
        SortType = type;
        await RefreshAsync();
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (!_hasMore || IsLoading)
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task SendReviewAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || IsSending || string.IsNullOrEmpty(SeriesId))
        {
            return;
        }

        if (!_apiClient.GetAuthenticationState().IsAuthenticated)
        {
            _appViewModel.ShowTipCommand.Execute(("请先登录后再发表剧评", InfoType.Warning));
            return;
        }

        IsSending = true;
        OnPropertyChanged(nameof(CanSendReview));
        string formatted = EmojiHelper.FormatEmojiText(content.Trim());
        try
        {
            double sendRating = Math.Clamp(Math.Round(Rating * 2.0) / 2.0, 0.5, 5.0);
            PhoenixOperationResult result = await _apiClient.SendDramaReviewAsync(
                SeriesId,
                sendRating,
                formatted
            );
            if (result.Succeeded)
            {
                _appViewModel.ShowTipCommand.Execute(("剧评发表成功", InfoType.Success));
                await RefreshAsync();
            }
            else
            {
                _appViewModel.ShowTipCommand.Execute(
                    (result.Message ?? "发表失败", InfoType.Error)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送剧评失败.");
            _appViewModel.ShowTipCommand.Execute(("发送剧评失败", InfoType.Error));
        }
        finally
        {
            IsSending = false;
            OnPropertyChanged(nameof(CanSendReview));
        }
    }

    [RelayCommand]
    private void BackToMain() => SelectedItem = null;

    private void ShowReviewDetail(DramaReviewItemViewModel item)
    {
        CommentItemViewModel commentItemVm = new(
            item.Data,
            SeriesId,
            videoId: null,
            _apiClient,
            isDramaReview: true
        );

        SelectedItem = new CommentDetailViewModel(
            commentItemVm,
            showMoreAction: _ => { },
            _dispatcherQueue,
            Ioc.Default.GetRequiredService<ILogger<CommentDetailViewModel>>(),
            _apiClient,
            _appViewModel,
            SeriesId,
            videoId: null
        );
        SelectedItem.InitializeCommand.Execute(null);
    }

    private async Task LoadItemsAsync()
    {
        if (string.IsNullOrEmpty(SeriesId) || IsLoading)
        {
            return;
        }

        IsLoading = true;
        UpdateLoginState();
        try
        {
            CommentPage page = await _apiClient.GetDramaReviewsAsync(
                SeriesId,
                count: 20,
                cursor: _cursor,
                sort: SortType
            );
            _cursor = page.Cursor;
            _hasMore = page.HasMore;
            TotalCount = page.Total;

            foreach (DramaComment item in page.Items)
            {
                if (!Reviews.Any(r => r.Id == item.Id))
                {
                    Reviews.Add(
                        new DramaReviewItemViewModel(
                            item,
                            SeriesId,
                            _apiClient,
                            _appViewModel,
                            _logger,
                            ShowReviewDetail
                        )
                    );
                }
            }

            IsEmpty = Reviews.Count == 0;
            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载剧评失败.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Clear()
    {
        _cursor = string.Empty;
        _hasMore = true;
        Reviews.Clear();
        IsEmpty = false;
        SelectedItem = null;
    }
}
