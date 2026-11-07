using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 短剧多维筛选索引视图模型.
/// </summary>
public sealed partial class EntertainmentIndexViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<EntertainmentIndexViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private int _offset;
    private bool _hasMore;
    private string? _sessionId;
    private CancellationTokenSource? _cancellationTokenSource;

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

    /// <inheritdoc/>
    public PgcSectionType SectionType { get; }

    /// <summary>
    /// 短剧剧集列表.
    /// </summary>
    public ObservableCollection<SeasonItemViewModel> Items { get; } = new();
}
