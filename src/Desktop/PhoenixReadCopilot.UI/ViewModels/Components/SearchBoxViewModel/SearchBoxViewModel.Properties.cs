using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Search;
using Richasy.BiliKernel.Models.Search;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 搜索视图模型.
/// </summary>
public sealed partial class SearchBoxViewModel
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchBoxViewModel> _logger;
    private readonly List<SearchRecommendItem> _recommendItems = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private string _extraRegionId = string.Empty;
    private string _extraRegionName = string.Empty;

    [ObservableProperty]
    public partial string Keyword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsHotSearchLoading { get; set; }

    /// <summary>
    /// 搜索建议.
    /// </summary>
    public ObservableCollection<SearchSuggestItemViewModel> Suggestion { get; } = new();

    /// <summary>
    /// 热搜列表.
    /// </summary>
    public ObservableCollection<HotSearchItem> HotSearchItems { get; } = new();
}
