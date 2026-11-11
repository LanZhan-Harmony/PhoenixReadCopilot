using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Models.Search;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频搜索分区详情视图模型.
/// </summary>
public sealed partial class VideoSearchSectionDetailViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<VideoSearchSectionDetailViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    private bool _canRequest;
    private int? _currentPage;
    private string _keyword;
    private bool _isPreventLoadMore;
    private SearchPartition? _partition;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial ComprehensiveSearchSortType Sort { get; set; }

    [ObservableProperty]
    public partial List<ComprehensiveSearchSortType> Sorts { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <inheritdoc/>
    public SearchSectionType SectionType => SearchSectionType.Drama;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Items { get; } = [];
}
