using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Comment;
using Richasy.BiliKernel.Models;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 评论详情视图模型.
/// </summary>
public sealed partial class CommentDetailViewModel : ViewModelBase<CommentItemViewModel>
{
    /// <summary>
    /// Bili 评论详情构造函数（保留兼容）.
    /// </summary>
    public CommentDetailViewModel(
        CommentItemViewModel data,
        Action<CommentDetailViewModel> showMoreAction,
        ICommentService service,
        DispatcherQueue dispatcherQueue,
        ILogger<CommentDetailViewModel> logger
    )
        : base(data)
    {
        _service = service;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
        _showMoreAction = showMoreAction;
    }

    /// <summary>
    /// 红果短剧评论详情构造函数.
    /// </summary>
    public CommentDetailViewModel(
        CommentItemViewModel data,
        Action<CommentDetailViewModel> showMoreAction,
        ICommentService? service,
        DispatcherQueue dispatcherQueue,
        ILogger<CommentDetailViewModel> logger,
        PhoenixReadApiClient? apiClient,
        AppViewModel? appViewModel,
        string? seriesId,
        string? videoId
    )
        : base(data)
    {
        _service = service;
        _dispatcherQueue = dispatcherQueue;
        _logger = logger;
        _showMoreAction = showMoreAction;
        _apiClient = apiClient;
        _appViewModel = appViewModel;
        _seriesId = seriesId;
        _videoId = videoId;
    }

    [RelayCommand]
    private void Initialize()
    {
        if (Comments.Count > 0)
        {
            return;
        }

        LoadItemsCommand.Execute(default);
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (IsLoading || _preventLoadMore || IsEmpty)
        {
            return;
        }

        // 优先使用红果分集评论二级回复
        if (_apiClient is not null && !string.IsNullOrEmpty(_seriesId) && !string.IsNullOrEmpty(_videoId))
        {
            try
            {
                IsLoading = true;
                var replyPage = await _apiClient.GetEpisodeCommentRepliesAsync(
                    Data.Data.Id,
                    _seriesId,
                    _videoId,
                    count: 20,
                    cursor: _cursor
                );

                _cursor = replyPage.Cursor;
                _preventLoadMore = !replyPage.HasMore;

                foreach (var item in replyPage.Items)
                {
                    if (Comments.Any(p => p.Data.Id == item.Id))
                    {
                        continue;
                    }

                    Comments.Add(new CommentItemViewModel(item, _seriesId, _videoId, _apiClient, SetReplyTarget));
                }

                ListUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载红果评论回复详情时出错");
                _preventLoadMore = true;
            }
            finally
            {
                IsEmpty = Comments.Count == 0;
                IsLoading = false;
            }

            return;
        }

        if (_service is null)
        {
            return;
        }

        try
        {
            IsLoading = true;
            var targetId = Data.Data.CommentId;
            var targetType = Data.Data.CommentType;
            var view = await _service.GetDetailCommentsAsync(
                targetId,
                targetType,
                CommentSortType.Time,
                Data.Data.Id,
                _offset
            );
            _offset = view.NextOffset;
            _preventLoadMore = view.NextOffset == 0 || view.Comments is null;
            foreach (var item in view.Comments)
            {
                if (Comments.Any(p => p.Data.Equals(item)))
                {
                    continue;
                }

                Comments.Add(new CommentItemViewModel(item, _service, SetReplyTarget));
            }

            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载评论详情时出错");
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
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            async () => await LoadItemsAsync()
        );
    }

    [RelayCommand]
    private void Clear()
    {
        Comments.Clear();
        _offset = 0;
        _cursor = string.Empty;
        _preventLoadMore = false;
        IsEmpty = false;
        ResetReplyTarget();
    }

    [RelayCommand]
    private void ShowMore()
    {
        _showMoreAction?.Invoke(this);
        InitializeCommand.Execute(default);
    }

    [RelayCommand]
    private async Task SendReplyAsync(string content)
    {
        if (IsReplying || string.IsNullOrEmpty(content))
        {
            return;
        }

        content = content.Trim();

        // 红果短剧回复
        if (_apiClient is not null && !string.IsNullOrEmpty(_videoId))
        {
            if (!_apiClient.GetAuthenticationState().IsAuthenticated)
            {
                _appViewModel?.ShowTipCommand.Execute(("请先登录后再发表回复", InfoType.Warning));
                return;
            }

            IsReplying = true;
            try
            {
                var targetUserId = _replyItem?.Data.User?.User?.Id ?? Data.Data.User?.User?.Id ?? string.Empty;
                var res = await _apiClient.SendCommentReplyAsync(
                    Data.Data.Id,
                    _videoId,
                    content,
                    targetUserId
                );

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
                _logger.LogError(ex, "发送红果评论回复失败");
                _appViewModel?.ShowTipCommand.Execute(("发送回复失败", InfoType.Error));
            }
            finally
            {
                IsReplying = false;
            }

            return;
        }

        if (_service is null)
        {
            return;
        }

        IsReplying = true;
        var targetIdBili = Data.Data.CommentId;
        var rootId = _replyItem is null ? Data.Data.Id : _replyItem.Data.RootId;
        var replyCommentId = _replyItem is null ? rootId : _replyItem.Data.Id;
        try
        {
            await _service.SendTextCommentAsync(
                content,
                targetIdBili,
                Data.Data.CommentType,
                rootId,
                replyCommentId
            );
            await Task.Delay(500);
            Refresh();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回复评论时出错");
        }
        finally
        {
            IsReplying = false;
        }
    }

    [RelayCommand]
    private void ResetReplyTarget()
    {
        _replyItem = default;
        ReplyTarget = default!;
    }

    private void SetReplyTarget(CommentItemViewModel item)
    {
        _replyItem = item;
        ReplyTarget = item?.Data.User?.User?.Name ?? string.Empty;
    }
}
