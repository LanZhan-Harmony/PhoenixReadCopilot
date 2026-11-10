using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 历史记录页面视图模型.
/// </summary>
public sealed partial class HistoryPageViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<HistoryPageViewModel> _logger;
    private readonly AppViewModel _appViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILoggerFactory _loggerFactory;
    private int _searchOffset;
    private bool _preventLoadMoreSearch;
    private CancellationTokenSource? _searchCancellationTokenSource;

    [ObservableProperty]
    public partial IHistorySectionDetailViewModel? SelectedSection { get; set; }

    [ObservableProperty]
    public partial List<IHistorySectionDetailViewModel>? Sections { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial bool IsSearchMode { get; set; }

    [ObservableProperty]
    public partial string SearchKeyword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSearchEmpty { get; set; }

    /// <summary>
    /// 初始化完成.
    /// </summary>
    public event EventHandler? SectionInitialized;

    /// <summary>
    /// 搜索更新.
    /// </summary>
    public event EventHandler? SearchUpdated;

    /// <summary>
    /// 搜索视频结果.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> SearchVideos { get; } = [];
}
