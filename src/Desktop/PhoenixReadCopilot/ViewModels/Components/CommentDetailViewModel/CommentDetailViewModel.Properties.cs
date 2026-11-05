using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Comment;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 评论详情视图模型.
/// </summary>
public sealed partial class CommentDetailViewModel
{
    private readonly ICommentService? _service;
    private readonly ILogger<CommentDetailViewModel> _logger;
    private readonly Action<CommentDetailViewModel>? _showMoreAction;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly PhoenixReadApiClient? _apiClient;
    private readonly AppViewModel? _appViewModel;
    private readonly string? _seriesId;
    private readonly string? _videoId;

    private bool _preventLoadMore;
    private long _offset;
    private string _cursor = string.Empty;
    private CommentItemViewModel? _replyItem;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ReplyTarget { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsReplying { get; set; }

    /// <summary>
    /// 列表已更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <summary>
    /// 评论列表.
    /// </summary>
    public ObservableCollection<CommentItemViewModel> Comments { get; } = new();
}
