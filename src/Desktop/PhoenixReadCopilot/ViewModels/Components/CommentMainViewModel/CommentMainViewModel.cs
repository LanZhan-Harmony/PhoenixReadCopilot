using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Comment;
using Richasy.BiliKernel.Models;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

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
        ICommentService service,
        ILogger<CommentMainViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ILoggerFactory loggerFactory,
        PhoenixReadApiClient? apiClient = default,
        AppViewModel? appViewModel = default
    )
    {
        _service = service;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
        _loggerFactory = loggerFactory;
        _apiClient = apiClient;
        _appViewModel = appViewModel;

        Sorts = Enum.GetValues<CommentSortType>().ToList();
        UpdateLoginState();
    }

    /// <summary>
    /// 初始化红果短剧分集评论.
    /// </summary>
    public void Initialize(
        string seriesId,
        string videoId,
        CommentSortOrder sortOrder = CommentSortOrder.Hot
    )
    {
        Clear();
        _seriesId = seriesId;
        _videoId = videoId;
        Id = videoId;
        SortType =
            sortOrder == CommentSortOrder.Latest ? CommentSortType.Time : CommentSortType.Hot;
        UpdateLoginState();
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 初始化红果短剧分集评论（兼容模式）.
    /// </summary>
    public void Initialize(string seriesId, string videoId, CommentSortType sortType)
    {
        Clear();
        _seriesId = seriesId;
        _videoId = videoId;
        Id = videoId;
        SortType = sortType;
        UpdateLoginState();
        Initialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 重置评论源（兼容老接口）.
    /// </summary>
    public void Initialize(string sourceId, CommentTargetType targetType, CommentSortType sortType)
    {
        Clear();
        Id = sourceId;
        _targetType = targetType;
        SortType = sortType;
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
        var targetSeriesId = _seriesId;
        var targetVideoId = _videoId;
        var targetId = Id;
        var targetType = _targetType;
        var sortType = SortType;

        Clear();

        _seriesId = targetSeriesId;
        _videoId = targetVideoId;
        Id = targetId;
        _targetType = targetType;
        SortType = sortType;

        await LoadItemsAsync();
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        if (string.IsNullOrEmpty(Id) || IsLoading || _preventLoadMore || IsEmpty)
        {
            return;
        }

        UpdateLoginState();

        // 优先使用红果分集评论接口
        if (
            _apiClient is not null
            && !string.IsNullOrEmpty(_seriesId)
            && !string.IsNullOrEmpty(_videoId)
        )
        {
            try
            {
                IsLoading = true;
                var sortOrder =
                    SortType == CommentSortType.Time
                        ? CommentSortOrder.Latest
                        : CommentSortOrder.Hot;
                var page = await _apiClient.GetEpisodeCommentsAsync(
                    _seriesId,
                    _videoId,
                    count: 20,
                    cursor: _cursor,
                    sort: sortOrder
                );
                _cursor = page.Cursor;
                _preventLoadMore = !page.HasMore;
                TopItem = null;

                foreach (var item in page.Items)
                {
                    if (Comments.Any(p => p.Data.Data.Id == item.Id))
                    {
                        continue;
                    }

                    var itemVm = new CommentItemViewModel(
                        item,
                        _seriesId,
                        _videoId,
                        _apiClient,
                        SetReplyTarget
                    );
                    Comments.Add(
                        new CommentDetailViewModel(
                            itemVm,
                            ShowMore,
                            _service,
                            _dispatcherQueue,
                            _loggerFactory.CreateLogger<CommentDetailViewModel>(),
                            _apiClient,
                            _appViewModel,
                            _seriesId,
                            _videoId
                        )
                    );
                }

                ListUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "尝试加载红果分集评论时失败");
                _preventLoadMore = true;
            }
            finally
            {
                IsLoading = false;
                IsEmpty = TopItem is null && Comments.Count == 0;
            }

            return;
        }

        try
        {
            IsLoading = true;
            var view = await _service.GetCommentsAsync(Id, _targetType, SortType, _offset);
            _preventLoadMore = view.IsEnd;
            if (view.TopComment is not null)
            {
                TopItem = new CommentDetailViewModel(
                    new CommentItemViewModel(view.TopComment, _service, SetReplyTarget),
                    ShowMore,
                    _service,
                    _dispatcherQueue,
                    _loggerFactory.CreateLogger<CommentDetailViewModel>()
                );
            }

            foreach (var item in view.Comments)
            {
                if (!Comments.Any(p => p.Data.Data.Equals(item)))
                {
                    Comments.Add(
                        new CommentDetailViewModel(
                            new CommentItemViewModel(item, _service, SetReplyTarget),
                            ShowMore,
                            _service,
                            _dispatcherQueue,
                            _loggerFactory.CreateLogger<CommentDetailViewModel>()
                        )
                    );
                }
            }

            _offset = view.NextOffset;
            ListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试加载评论时失败");
            _preventLoadMore = true;
        }
        finally
        {
            IsLoading = false;
            IsEmpty = TopItem is null && Comments.Count == 0;
        }
    }

    [RelayCommand]
    private async Task ChangeSortTypeAsync(CommentSortType type)
    {
        SortType = type;
        await RefreshAsync();
    }

    [RelayCommand]
    private void BackToMain() => SelectedItem = default;

    [RelayCommand]
    private async Task SendReplyAsync(string content)
    {
        if (IsReplying || string.IsNullOrEmpty(content))
        {
            return;
        }

        content = content.Trim();

        // 红果分集发送/回复评论
        if (
            _apiClient is not null
            && !string.IsNullOrEmpty(_seriesId)
            && !string.IsNullOrEmpty(_videoId)
        )
        {
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
                    result = await _apiClient.SendEpisodeCommentAsync(_seriesId, _videoId, content);
                }
                else
                {
                    result = await _apiClient.SendEpisodeCommentReplyAsync(
                        commentId: _replyItem.Data.Id,
                        seriesId: _seriesId,
                        videoId: _videoId,
                        text: content,
                        replyToUserId: _replyItem.Data.User?.User?.Id ?? string.Empty
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
                _logger.LogError(ex, "发送红果评论失败");
                _appViewModel?.ShowTipCommand.Execute(("发送评论失败", InfoType.Error));
            }
            finally
            {
                IsReplying = false;
            }

            return;
        }

        IsReplying = true;
        var replyCommentId = _replyItem is null ? "0" : _replyItem.Data.Id;
        try
        {
            await _service.SendTextCommentAsync(content, Id, _targetType, "0", replyCommentId);
            if (SortType == CommentSortType.Time)
            {
                await Task.Delay(500);
                await RefreshAsync();
            }
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
        _replyItem = null;
        ReplyTarget = default!;
    }

    private void SetReplyTarget(CommentItemViewModel item)
    {
        _replyItem = item;
        ReplyTarget = item?.Data.User?.User?.Name ?? string.Empty;
    }

    private void ShowMore(CommentDetailViewModel vm) => SelectedItem = vm;

    private void Clear()
    {
        TopItem?.ClearCommand.Execute(default);
        if (Comments is not null)
        {
            foreach (var item in Comments)
            {
                item.ClearCommand.Execute(default);
            }
        }

        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Comments.Clear);
        _offset = 0;
        _cursor = string.Empty;
        _preventLoadMore = false;
        Id = default!;
        _seriesId = string.Empty;
        _videoId = string.Empty;
        _targetType = default;
        SortType = default;
        IsEmpty = false;
        TopItem = default;
        SelectedItem = default;
        ResetReplyTarget();
    }
}
