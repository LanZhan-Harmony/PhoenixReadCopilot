using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Search;
using Richasy.BiliKernel.Models.Search;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 搜索视图模型.
/// </summary>
public sealed partial class SearchBoxViewModel
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchBoxViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly AppSettings _settings;
    private readonly List<SearchRecommendItem> _recommendItems = [];
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
    public ObservableCollection<SearchSuggestItemViewModel> Suggestion { get; } = [];

    /// <summary>
    /// 热搜列表.
    /// </summary>
    public ObservableCollection<HotSearchItem> HotSearchItems { get; } = [];
}
