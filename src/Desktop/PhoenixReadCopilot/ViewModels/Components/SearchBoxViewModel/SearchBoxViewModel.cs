using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Search;
using Richasy.BiliKernel.Models.Search;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 搜索视图模型.
/// </summary>
public sealed partial class SearchBoxViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchBoxViewModel"/> class.
    /// </summary>
    public SearchBoxViewModel(
        ISearchService searchService,
        NavigationViewModel navigationViewModel,
        ISettingsService settingsService,
        ILogger<SearchBoxViewModel> logger
    )
    {
        _searchService = searchService;
        _navigationViewModel = navigationViewModel;
        _settings = settingsService.Settings;
        _logger = logger;
        _navigationViewModel.OverlayNavigated += OnOverlayNavigated;
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

    [RelayCommand]
    private async Task ReloadSuggestionsAsync()
    {
        TryCancelSuggest();

        if (string.IsNullOrEmpty(Keyword.Trim()))
        {
            if (string.IsNullOrEmpty(_extraRegionId))
            {
                bool isRecommendEnabled = _settings.ShowSearchRecommend;
                if (!isRecommendEnabled)
                {
                    _recommendItems.Clear();
                    Suggestion.Clear();
                    return;
                }

                if (_recommendItems.Count == 0)
                {
                    IReadOnlyList<SearchRecommendItem> recommends =
                        await _searchService.GetSearchRecommendsAsync();
                    _recommendItems.AddRange(recommends);
                }

                if (Suggestion.Count == _recommendItems.Count)
                {
                    return;
                }

                Suggestion.Clear();
                foreach (SearchRecommendItem item in _recommendItems)
                {
                    Suggestion.Add(new(new SearchSuggestItem(item.Text, item.Keyword)));
                }
            }
            else
            {
                Suggestion.Clear();
                Suggestion.Add(new("global", Resources.WholePartitions));
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
                    IReadOnlyList<SearchSuggestItem>? suggests =
                        await _searchService.GetSearchSuggestsAsync(
                            Keyword,
                            _cancellationTokenSource.Token
                        );
                    if (suggests is not null)
                    {
                        foreach (SearchSuggestItem item in suggests)
                        {
                            Suggestion.Add(new(item));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "尝试加载搜索建议时出错.");
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

    [RelayCommand]
    private async Task LoadHotSearchAsync()
    {
        if (HotSearchItems.Count > 0)
        {
            return;
        }

        IsHotSearchLoading = true;
        try
        {
            IReadOnlyList<HotSearchItem> hotSearches = await _searchService.GetTotalHotSearchAsync(
                30
            );
            foreach (HotSearchItem item in hotSearches)
            {
                HotSearchItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试加载热搜榜单时出错.");
        }

        IsHotSearchLoading = false;
    }

    [RelayCommand]
    private void TryCancelSuggest()
    {
        if (_cancellationTokenSource is not null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void Search(SearchSuggestItemViewModel vm)
    {
        string keyword = vm.Keyword;
        if (string.IsNullOrEmpty(keyword))
        {
            return;
        }

        if (string.IsNullOrEmpty(vm.RegionId) || vm.RegionId == "global")
        {
            _navigationViewModel.Search(keyword);
        }
        else
        {
            _navigationViewModel.SearchInRegion(keyword);
        }
    }
}
