using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Components;
using Richasy.BiliKernel.Bili.Article;
using Richasy.BiliKernel.Models.Article;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 文章阅读器页面视图模型.
/// </summary>
public sealed partial class ArticleReaderPageViewModel
{
    private readonly IArticleDiscoveryService _discoveryService;
    private readonly IArticleOperationService _operationService;
    private readonly ILogger<ArticleReaderPageViewModel> _logger;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string? Author { get; set; }

    [ObservableProperty]
    public partial Uri? Avatar { get; set; }

    [ObservableProperty]
    public partial bool IsVip { get; set; }

    [ObservableProperty]
    public partial bool IsFollowed { get; set; }

    [ObservableProperty]
    public partial bool? IsLiked { get; set; }

    [ObservableProperty]
    public partial bool? IsFavorited { get; set; }

    [ObservableProperty]
    public partial int LikeCount { get; set; }

    [ObservableProperty]
    public partial int CommentCount { get; set; }

    [ObservableProperty]
    public partial int FavoriteCount { get; set; }

    [ObservableProperty]
    public partial bool IsCommentsOpened { get; set; }

    /// <summary>
    /// 文章已加载.
    /// </summary>
    public event EventHandler ArticleInitialized;

    /// <summary>
    /// 文章标识.
    /// </summary>
    public ArticleIdentifier? Article { get; private set; }

    /// <summary>
    /// 文章内容.
    /// </summary>
    public ArticleDetail? Content { get; private set; }

    /// <summary>
    /// 评论模块.
    /// </summary>
    public CommentMainViewModel CommentModule { get; }
}
