using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Comment;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 评论主视图模型.
/// </summary>
public sealed partial class CommentMainViewModel
{
    private readonly ICommentService _service;
    private readonly ILogger<CommentMainViewModel> _logger;

    private bool _preventLoadMore;
    private CommentTargetType _targetType;
    private long _offset;
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
    public partial List<CommentSortType> Sorts { get; set; }

    [ObservableProperty]
    public partial CommentSortType SortType { get; set; }

    [ObservableProperty]
    public partial string ReplyTarget { get; set; }

    [ObservableProperty]
    public partial bool IsReplying { get; set; }

    /// <summary>
    /// 已初始化.
    /// </summary>
    public event EventHandler Initialized;

    /// <summary>
    /// 列表已更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <summary>
    /// 评论列表.
    /// </summary>
    public ObservableCollection<CommentDetailViewModel> Comments { get; } = new();

    /// <summary>
    /// 标识符.
    /// </summary>
    public string Id { get; private set; }

    /// <inheritdoc/>
    public string Title { get; } = Resources.Comments;
}
