using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Comment;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 评论详情视图模型.
/// </summary>
public sealed partial class CommentDetailViewModel
{
    private readonly ICommentService _service;
    private readonly ILogger<CommentDetailViewModel> _logger;
    private readonly Action<CommentDetailViewModel> _showMoreAction;

    private bool _preventLoadMore;
    private long _offset;
    private CommentItemViewModel? _replyItem;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string ReplyTarget { get; set; }

    [ObservableProperty]
    public partial bool IsReplying { get; set; }

    /// <summary>
    /// 列表已更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <summary>
    /// 评论列表.
    /// </summary>
    public ObservableCollection<CommentItemViewModel> Comments { get; } = new();
}
