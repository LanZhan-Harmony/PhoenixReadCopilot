using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 文章历史记录详情视图模型.
/// </summary>
public sealed partial class ArticleHistorySectionDetailViewModel
{
    private readonly IViewHistoryService _service;
    private readonly ILogger<ArticleHistorySectionDetailViewModel> _logger;

    private bool _isPreventLoadMore;
    private long _offset;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ArticleItemViewModel> Items { get; set; } = new();

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <inheritdoc/>
    public ViewHistoryTabType Type => ViewHistoryTabType.Article;
}
