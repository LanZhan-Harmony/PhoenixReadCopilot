using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 多维筛选页面视图模型.
/// </summary>
public sealed partial class FilterPageViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<FilterPageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private int _offset;
    private bool _hasMore;
    private string? _sessionId;
    private CancellationTokenSource? _filterCts;
    private CancellationTokenSource? _indexCts;

    [ObservableProperty]
    public partial List<IndexFilterViewModel>? Filters { get; set; }

    [ObservableProperty]
    public partial bool IsFilterLoading { get; set; }

    [ObservableProperty]
    public partial bool IsSeasonLoading { get; set; }

    /// <summary>
    /// 条目已更新.
    /// </summary>
    public event EventHandler? ItemsUpdated;

    /// <summary>
    /// 短剧剧集列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Items { get; } = [];
}
