using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 评论主视图模型.
/// </summary>
public sealed partial class CommentMainViewModel
{
    private readonly ILogger<CommentMainViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILoggerFactory _loggerFactory;
    private readonly PhoenixReadApiClient? _apiClient;
    private readonly AppViewModel? _appViewModel;

    private bool _preventLoadMore;
    private string _cursor = string.Empty;
    private string _seriesId = string.Empty;
    private string _videoId = string.Empty;
    private Items.CommentItemViewModel? _replyItem;

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
}
