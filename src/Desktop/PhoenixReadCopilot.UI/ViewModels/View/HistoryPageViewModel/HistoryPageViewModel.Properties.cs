using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Search;
using Richasy.BiliKernel.Bili.User;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 历史记录页面视图模型.
/// </summary>
public sealed partial class HistoryPageViewModel
{
    private readonly IViewHistoryService _service;
    private readonly ISearchService _searchService;
    private readonly ILogger<HistoryPageViewModel> _logger;
    private int _searchPn;
    private bool _preventLoadMoreSearch;
    private CancellationTokenSource _searchCancellationTokenSource;

    [ObservableProperty]
    public partial IHistorySectionDetailViewModel SelectedSection { get; set; }

    [ObservableProperty]
    public partial List<IHistorySectionDetailViewModel> Sections { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial bool IsSearchMode { get; set; }

    [ObservableProperty]
    public partial string SearchKeyword { get; set; }

    [ObservableProperty]
    public partial bool IsSearchEmpty { get; set; }

    /// <summary>
    /// 初始化完成.
    /// </summary>
    public event EventHandler SectionInitialized;

    /// <summary>
    /// 搜索更新.
    /// </summary>
    public event EventHandler SearchUpdated;

    /// <summary>
    /// 搜索视频结果.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> SearchVideos { get; } = new();
}
