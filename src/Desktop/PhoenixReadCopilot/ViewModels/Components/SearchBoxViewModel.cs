using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Search;
using PhoenixReadCopilot.Core.Models.Settings;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;
using ZLogger;
using HotSearchItem = PhoenixReadCopilot.Core.Models.Search.HotSearchItem;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 搜索视图模型.
/// </summary>
public sealed partial class SearchBoxViewModel : ViewModelBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchBoxViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly AppSettings _settings;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchBoxViewModel"/> class.
    /// </summary>
    public SearchBoxViewModel(
        ISearchService searchService,
        INavigationService navigationService,
        ISettingsService settingsService,
        ILogger<SearchBoxViewModel> logger
    )
    {
        _searchService = searchService;
        _navigationService = navigationService;
        _settings = settingsService.Settings;
        _logger = logger;
        _navigationService.OverlayNavigated += OnOverlayNavigated;
    }

    private void OnOverlayNavigated(object? sender, Type sourcePageType)
    {
        if (sourcePageType == typeof(UserSpacePage))
        {
            SetExtraRegion("space", Resources.UserSpace);
        }
        else if (sourcePageType == typeof(HistoryPage))
        {
            SetExtraRegion("history", Resources.ViewHistory);
        }
        else
        {
            SetExtraRegion(string.Empty, string.Empty);
            Keyword = string.Empty;
        }
    }

    /// <summary>
    /// 设置附加搜索区域（用于在特殊页面进行局部搜索）.
    /// </summary>
    public void SetExtraRegion(string regionId, string regionName)
    {
        _extraRegionId = regionId;
        _extraRegionName = regionName;
    }

    public async Task ReloadSuggestionsAsync()
    {
        TryCancelSuggest();

        if (string.IsNullOrEmpty(Keyword.Trim()))
        {
            Suggestion.Clear();
            if (string.IsNullOrEmpty(_extraRegionId))
            {
                if (_settings.ShowSearchRecommend)
                {
                    try
                    {
                        IReadOnlyList<string> history = await _searchService.GetSearchHistoryAsync();
                        foreach (string item in history.Take(8))
                        {
                            Suggestion.Add(new(item));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ZLogWarning(ex, $"尝试获取搜索历史时失败.");
                    }
                }
            }
            else
            {
                Suggestion.Add(new(_extraRegionId, _extraRegionName));
            }
        }
        else
        {
            if (string.IsNullOrEmpty(_extraRegionId))
            {
                try
                {
                    Suggestion.Clear();
                    _cancellationTokenSource = new CancellationTokenSource();
                    IReadOnlyList<SearchSuggestion> suggests = await _searchService.GetSuggestionsAsync(
                        Keyword.Trim(),
                        maxResults: 8,
                        _cancellationTokenSource.Token
                    );

                    if (suggests is not null)
                    {
                        foreach (SearchSuggestion item in suggests)
                        {
                            Suggestion.Add(new(item.Name));
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Ignore cancellation
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"尝试加载搜索建议时出错.");
                }
            }
            else
            {
                foreach (SearchSuggestItemViewModel item in Suggestion)
                {
                    item.SearchContent = Keyword;
                    item.Keyword = Keyword;
                }
            }
        }
    }

    public async Task LoadHotSearchAsync()
    {
        if (HotSearchItems.Count > 0)
        {
            return;
        }

        IsHotSearchLoading = true;
        try
        {
            IReadOnlyList<HotSearchItem> items = await _searchService.GetHotSearchesAsync();
            foreach (HotSearchItem item in items)
            {
                HotSearchItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载热搜榜单时出错.");
        }
        finally
        {
            IsHotSearchLoading = false;
        }
    }

    public void TryCancelSuggest()
    {
        if (_cancellationTokenSource is not null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 根据关键词直接执行搜索.
    /// </summary>
    public void Search(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return;
        }

        Search(new SearchSuggestItemViewModel(new SearchSuggestItem(keyword, keyword)));
    }

    public void Search(SearchSuggestItemViewModel vm)
    {
        string keyword = vm.Keyword;
        if (string.IsNullOrEmpty(keyword))
        {
            return;
        }

        _ = _searchService.AddSearchHistoryAsync(keyword);

        if (string.IsNullOrEmpty(vm.RegionId) || vm.RegionId == "global")
        {
            _navigationService.Search(keyword);
        }
        else
        {
            _navigationService.SearchInRegion(keyword);
        }
    }
}
