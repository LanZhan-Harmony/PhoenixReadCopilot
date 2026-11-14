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
/// 评论详情视图模型.
/// </summary>
public sealed partial class CommentDetailViewModel : ViewModelBase<CommentItemViewModel>
{
    /// <summary>
    /// 红果短剧评论详情构造函数.
    /// </summary>
    public CommentDetailViewModel(
        CommentItemViewModel data,
        Action<CommentDetailViewModel>? showMoreAction,
        DispatcherQueue dispatcherQueue,
        ILogger<CommentDetailViewModel> logger,
        PhoenixReadApiClient? apiClient,
        AppViewModel? appViewModel,
        string? dramaId,
        string? videoId
    )
        : base(data)
    {
        _showMoreAction = showMoreAction;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
        _apiClient = apiClient;
        _appViewModel = appViewModel;
        _dramaId = dramaId;
        _videoId = videoId;
    }

    [RelayCommand]
    private void Initialize()
    {
        if (Comments.Count > 0)
        {
            return;
        }

        LoadItemsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading || _preventLoadMore || IsEmpty || _apiClient is null)
        {
            return;
        }

        string? effectiveDramaId = !string.IsNullOrEmpty(_dramaId) ? _dramaId : Data.DramaId;
        if (string.IsNullOrEmpty(effectiveDramaId))
        {
            return;
        }

        IsLoading = true;
        try
        {
            CommentReplyPage replyPage;

            // 剧评回复还是分集评论回复
            if (Data.IsDramaReview || string.IsNullOrEmpty(_videoId))
            {
                replyPage = await _apiClient.GetDramaReviewRepliesAsync(
                    commentId: Data.Id,
                    dramaId: effectiveDramaId,
                    count: 20,
                    cursor: _cursor
                );
            }
            else
            {
                replyPage = await _apiClient.GetEpisodeCommentRepliesAsync(
                    commentId: Data.Id,
                    dramaId: effectiveDramaId,
                    videoId: _videoId,
                    count: 20,
                    cursor: _cursor
                );
            }

            _cursor = replyPage.Cursor;
            _preventLoadMore = !replyPage.HasMore;

            foreach (CommentReply item in replyPage.Items)
            {
                if (Comments.Any(p => p.Id == item.Id))
                {
                    continue;
                }

                Comments.Add(
                    new CommentItemViewModel(
                        item,
                        effectiveDramaId,
                        _videoId,
                        _apiClient,
                        isDramaReview: Data.IsDramaReview,
                        markReplyTargetAction: SetReplyTarget
                    )
                );
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"加载红果评论回复详情时出错");
            _preventLoadMore = true;
        }
        finally
        {
            IsEmpty = Comments.Count == 0;
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        Clear();
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
            await LoadItemsAsync()
        );
    }

    [RelayCommand]
    private void Clear()
    {
        Comments.Clear();
        _cursor = string.Empty;
        _preventLoadMore = false;
        IsEmpty = false;
        ResetReplyTarget();
    }

    [RelayCommand]
    private void ShowMore()
    {
        _showMoreAction?.Invoke(this);
        InitializeCommand.Execute(null);
    }

    [RelayCommand]
    private async Task SendReplyAsync(string content)
    {
        if (IsReplying || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        content = EmojiHelper.FormatEmojiText(content.Trim());

        if (_apiClient is null)
        {
            return;
        }

        if (!_apiClient.GetAuthenticationState().IsAuthenticated)
        {
            _appViewModel?.ShowTipCommand.Execute(("请先登录后再发表回复", InfoType.Warning));
            return;
        }

        string? effectiveDramaId = !string.IsNullOrEmpty(_dramaId) ? _dramaId : Data.DramaId;
        if (string.IsNullOrEmpty(effectiveDramaId))
        {
            return;
        }

        IsReplying = true;
        try
        {
            string targetUserId = _replyItem?.UserId ?? Data.UserId;
            PhoenixOperationResult res;

            if (Data.IsDramaReview || string.IsNullOrEmpty(_videoId))
            {
                // 回复整剧剧评
                res = await _apiClient.SendDramaReviewReplyAsync(
                    commentId: Data.Id,
                    dramaId: effectiveDramaId,
                    text: content,
                    replyToUserId: targetUserId
                );
            }
            else
            {
                // 回复分集评论
                res = await _apiClient.SendEpisodeCommentReplyAsync(
                    commentId: Data.Id,
                    dramaId: effectiveDramaId,
                    videoId: _videoId,
                    text: content,
                    replyToUserId: targetUserId
                );
            }

            if (res.Succeeded)
            {
                _appViewModel?.ShowTipCommand.Execute(("回复已发送", InfoType.Success));
                ResetReplyTarget();
                Refresh();
            }
            else
            {
                _appViewModel?.ShowTipCommand.Execute((res.Message ?? "发送失败", InfoType.Error));
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"发送红果评论回复失败");
            _appViewModel?.ShowTipCommand.Execute(("发送回复失败", InfoType.Error));
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
}
