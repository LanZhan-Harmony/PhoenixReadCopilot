using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 评论主视图模型.
/// </summary>
public sealed partial class CommentMainViewModel : ViewModelBase, IPlayerSectionDetailViewModel
{
    private readonly ILogger<CommentMainViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ICommentService _commentService;
    private readonly IAuthenticationService _authService;
    private readonly AppViewModel? _appViewModel;

    private bool _preventLoadMore;
    private string _cursor = string.Empty;
    private string _dramaId = string.Empty;
    private string _videoId = string.Empty;
    private CommentItemViewModel? _replyItem;

    [ObservableProperty]
    public partial CommentDetailViewModel? TopItem { get; set; }

    [ObservableProperty]
    public partial CommentDetailViewModel? SelectedItem { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsLoggedIn { get; set; }

    [ObservableProperty]
    public partial List<CommentSortOrder> Sorts { get; set; } =
        [CommentSortOrder.Hot, CommentSortOrder.Latest];

    [ObservableProperty]
    public partial CommentSortOrder SortType { get; set; } = CommentSortOrder.Hot;

    [ObservableProperty]
    public partial string ReplyTarget { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsReplying { get; set; }

    /// <summary>
    /// 已初始化.
    /// </summary>
    public event EventHandler? Initialized;

    /// <summary>
    /// 列表已更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <summary>
    /// 评论列表.
    /// </summary>
    public ObservableCollection<CommentDetailViewModel> Comments { get; } = [];

    /// <summary>
    /// 标识符.
    /// </summary>
    public string Id { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public string Title { get; } = Resources.Comments;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentMainViewModel"/> class.
    /// </summary>
    public CommentMainViewModel(
        ILogger<CommentMainViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ILoggerFactory loggerFactory,
        ICommentService commentService,
        IAuthenticationService authService,
        AppViewModel? appViewModel = null
    )
    {
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
        _loggerFactory = loggerFactory;
        _commentService = commentService;
        _authService = authService;
        _appViewModel = appViewModel;

        Sorts = [CommentSortOrder.Hot, CommentSortOrder.Latest];
        SortType = CommentSortOrder.Hot;
        UpdateLoginState();
    }

    /// <summary>
    /// 初始化红果短剧分集评论.
    /// </summary>
    public void Initialize(
        string dramaId,
        string videoId,
        CommentSortOrder sortOrder = CommentSortOrder.Hot
    )
    {
        Clear();
        _dramaId = dramaId;
        _videoId = videoId;
        Id = videoId;
        SortType = sortOrder;
        UpdateLoginState();
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 兼容模式初始化（供旧代码平滑调用）.
    /// </summary>
    public void Initialize(string sourceId, object? targetType = null, object? sortType = null)
    {
        Clear();
        Id = sourceId;
        UpdateLoginState();
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 刷新当前登录态.
    /// </summary>
    public void UpdateLoginState()
    {
        IsLoggedIn = _authService.IsAuthenticated;
    }

    public async Task TryFirstLoadAsync()
    {
        if (Comments.Count > 0)
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        string targetDramaId = _dramaId;
        string targetVideoId = _videoId;
        string targetId = Id;
        CommentSortOrder sortType = SortType;

        Clear();

        _dramaId = targetDramaId;
        _videoId = targetVideoId;
        Id = targetId;
        SortType = sortType;

        await LoadItemsAsync();
    }

    public async Task LoadItemsAsync()
    {
        if (
            string.IsNullOrEmpty(Id)
            || IsLoading
            || _preventLoadMore
            || IsEmpty
        )
        {
            return;
        }

        UpdateLoginState();

        if (string.IsNullOrEmpty(_dramaId) || string.IsNullOrEmpty(_videoId))
        {
            return;
        }

        try
        {
            IsLoading = true;
            CommentPage page = await _commentService.GetEpisodeCommentsAsync(
                _dramaId,
                _videoId,
                count: 20,
                cursor: _cursor,
                sort: SortType
            );
            _cursor = page.Cursor;
            _preventLoadMore = !page.HasMore;
            TopItem = null;

            foreach (Comment item in page.Items)
            {
                if (Comments.Any(p => p.Data.Id == item.Id))
                {
                    continue;
                }

                CommentItemViewModel itemVm = new(
                    item,
                    _dramaId,
                    _videoId,
                    _commentService,
                    _authService,
                    isDramaReview: false,
                    markReplyTargetAction: SetReplyTarget
                );
                Comments.Add(
                    new CommentDetailViewModel(
                        itemVm,
                        ShowMore,
                        _dispatcherQueue,
                        _loggerFactory.CreateLogger<CommentDetailViewModel>(),
                        _commentService,
                        _authService,
                        _appViewModel,
                        _dramaId,
                        _videoId
                    )
                );
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载红果分集评论时失败");
            _preventLoadMore = true;
        }
        finally
        {
            IsLoading = false;
            IsEmpty = TopItem is null && Comments.Count == 0;
        }
    }

    public async Task ChangeSortTypeAsync(CommentSortOrder type)
    {
        SortType = type;
        await RefreshAsync();
    }

    [RelayCommand]
    private void BackToMain() => SelectedItem = null;

    [RelayCommand]
    private async Task SendReplyAsync(string content)
    {
        if (IsReplying || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        content = EmojiHelper.FormatEmojiText(content.Trim());

        if (string.IsNullOrEmpty(_dramaId) || string.IsNullOrEmpty(_videoId))
        {
            return;
        }

        if (!_authService.IsAuthenticated)
        {
            _appViewModel?.ShowTip("请先登录后再发表评论", InfoType.Warning);
            return;
        }

        IsReplying = true;
        try
        {
            PhoenixOperationResult result;
            if (_replyItem is null)
            {
                result = await _commentService.SendEpisodeCommentAsync(_dramaId, _videoId, content);
            }
            else
            {
                result = await _commentService.SendEpisodeCommentReplyAsync(
                    commentId: _replyItem.Id,
                    dramaId: _dramaId,
                    videoId: _videoId,
                    text: content,
                    replyToUserId: _replyItem.UserId
                );
            }

            if (result.Succeeded)
            {
                _appViewModel?.ShowTip("评论已发表", InfoType.Success);
                ResetReplyTarget();
                await RefreshAsync();
            }
            else
            {
                _appViewModel?.ShowTip(result.Message ?? "发表失败", InfoType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"发送红果评论失败");
            _appViewModel?.ShowTip("发送评论失败", InfoType.Error);
        }
        finally
        {
            IsReplying = false;
        }
    }

    [RelayCommand]
    private void ResetReplyTarget()
    {
        _replyItem = null;
        ReplyTarget = string.Empty;
    }

    private void SetReplyTarget(CommentItemViewModel item)
    {
        _replyItem = item;
        ReplyTarget = item?.UserName ?? string.Empty;
    }

    private void ShowMore(CommentDetailViewModel vm) => SelectedItem = vm;

    private void Clear()
    {
        TopItem?.Clear();
        if (Comments is not null)
        {
            foreach (CommentDetailViewModel item in Comments)
            {
                item.Clear();
            }
        }

        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Comments.Clear);
        _cursor = string.Empty;
        _preventLoadMore = false;
        Id = string.Empty;
        _dramaId = string.Empty;
        _videoId = string.Empty;
        IsEmpty = false;
        TopItem = null;
        SelectedItem = null;
        ResetReplyTarget();
    }
}
