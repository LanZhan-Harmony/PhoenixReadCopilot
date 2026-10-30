using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Article;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 专栏文章分区详情视图模型.
/// </summary>
public sealed partial class ArticlePartitionDetailViewModel
{
    private readonly ILogger<ArticlePartitionDetailViewModel> _logger;
    private readonly IArticleDiscoveryService _service;
    private readonly Dictionary<string, List<ArticleItemViewModel>> _childPartitionArticleCache =
        new();
    private readonly Dictionary<string, int> _childPartitionOffsetCache = new();
    private readonly bool _isRecommendPartition;
    private readonly bool _isHotPartition;

    private List<ArticleItemViewModel> _recommendCache;
    private int _recommendOffset;
    private bool _preventLoadMore;

    [ObservableProperty]
    public partial List<ArticleSortType> SortTypes { get; set; }

    [ObservableProperty]
    public partial List<PartitionViewModel> Children { get; set; }

    [ObservableProperty]
    public partial ArticleSortType SelectedSortType { get; set; }

    [ObservableProperty]
    public partial bool IsRecommend { get; set; }

    [ObservableProperty]
    public partial bool IsArticleLoading { get; set; }

    [ObservableProperty]
    public partial PartitionViewModel CurrentPartition { get; set; }

    /// <summary>
    /// 初始化完成.
    /// </summary>
    public event EventHandler Initialized;

    /// <summary>
    /// 文章列表已完成更新.
    /// </summary>
    public event EventHandler ArticleListUpdated;

    /// <summary>
    /// 显示的文章列表.
    /// </summary>
    public ObservableCollection<ArticleItemViewModel> Articles { get; } = new();
}
