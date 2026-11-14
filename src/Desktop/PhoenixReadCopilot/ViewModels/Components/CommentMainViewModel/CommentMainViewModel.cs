using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Helpers;
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
    /// <summary>
    /// Initializes a new instance of the <see cref="CommentMainViewModel"/> class.
    /// </summary>
    public CommentMainViewModel(
        ILogger<CommentMainViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ILoggerFactory loggerFactory,
        PhoenixReadApiClient? apiClient = null,
        AppViewModel? appViewModel = null
    )
    {
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
        _loggerFactory = loggerFactory;
        _apiClient = apiClient;
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
        IsLoggedIn = _apiClient?.GetAuthenticationState().IsAuthenticated ?? false;
    }

    [RelayCommand]
    private async Task TryFirstLoadAsync()
    {
        if (Comments.Count > 0)
        {
            return;
        }

        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
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

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (
            string.IsNullOrEmpty(Id)
            || IsLoading
            || _preventLoadMore
            || IsEmpty
            || _apiClient is null
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
            CommentPage page = await _apiClient.GetEpisodeCommentsAsync(
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
                    _apiClient,
                    isDramaReview: false,
                    markReplyTargetAction: SetReplyTarget
                );
                Comments.Add(
                    new CommentDetailViewModel(
                        itemVm,
                        ShowMore,
                        _dispatcherQueue,
                        _loggerFactory.CreateLogger<CommentDetailViewModel>(),
                        _apiClient,
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

    [RelayCommand]
    private async Task ChangeSortTypeAsync(CommentSortOrder type)
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

        if (_apiClient is null || string.IsNullOrEmpty(_dramaId) || string.IsNullOrEmpty(_videoId))
        {
            return;
        }

        if (!_apiClient.GetAuthenticationState().IsAuthenticated)
        {
            _appViewModel?.ShowTipCommand.Execute(("请先登录后再发表评论", InfoType.Warning));
            return;
        }

        IsReplying = true;
        try
        {
            PhoenixOperationResult result;
            if (_replyItem is null)
            {
                result = await _apiClient.SendEpisodeCommentAsync(_dramaId, _videoId, content);
            }
            else
            {
                result = await _apiClient.SendEpisodeCommentReplyAsync(
                    commentId: _replyItem.Id,
                    dramaId: _dramaId,
                    videoId: _videoId,
                    text: content,
                    replyToUserId: _replyItem.UserId
                );
            }

            if (result.Succeeded)
            {
                _appViewModel?.ShowTipCommand.Execute(("评论已发表", InfoType.Success));
                ResetReplyTarget();
                await RefreshAsync();
            }
            else
            {
                _appViewModel?.ShowTipCommand.Execute(
                    (result.Message ?? "发表失败", InfoType.Error)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"发送红果评论失败");
            _appViewModel?.ShowTipCommand.Execute(("发送评论失败", InfoType.Error));
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
        TopItem?.ClearCommand.Execute(null);
        if (Comments is not null)
        {
            foreach (CommentDetailViewModel item in Comments)
            {
                item.ClearCommand.Execute(null);
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
