using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Models.Search;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using HotSearchItem = PhoenixReadCopilot.Models.Search.HotSearchItem;
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
        PhoenixReadApiClient apiClient,
        NavigationViewModel navigationViewModel,
        ISettingsService settingsService,
        ILogger<SearchBoxViewModel> logger
    )
    {
        _apiClient = apiClient;
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
                    try
                    {
                        IReadOnlyList<string> cues = await _apiClient.GetSearchCuesAsync();
                        _recommendItems.AddRange(cues);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "尝试获取搜索占位推荐词时失败.");
                    }
                }

                if (Suggestion.Count == _recommendItems.Count && Suggestion.Count > 0)
                {
                    return;
                }

                Suggestion.Clear();
                foreach (string item in _recommendItems)
                {
                    Suggestion.Add(new(item));
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
                    IReadOnlyList<SearchSuggestion> suggests =
                        await _apiClient.GetSuggestionsAsync(
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
            HotSearch hotSearch = await _apiClient.GetHotSearchAsync();
            int index = 1;

            // 优先加载短剧热搜榜，若无则使用第一张榜单或猜你想搜
            HotSearchBoard? board = hotSearch.Boards.FirstOrDefault(b => b.Title.Contains("短剧"))
                ?? hotSearch.Boards.FirstOrDefault();

            IEnumerable<PhoenixReadApi.HotSearchItem> items = board?.Items ?? hotSearch.Guesses;
            foreach (PhoenixReadApi.HotSearchItem item in items)
            {
                SearchItemIcon? icon = null;
                if (!string.IsNullOrEmpty(item.CoverUrl) && Uri.TryCreate(item.CoverUrl, UriKind.Absolute, out Uri? coverUri))
                {
                    icon = new SearchItemIcon { Uri = coverUri };
                }

                string heat = !string.IsNullOrEmpty(item.CardTips) ? item.CardTips : item.Heat;
                HotSearchItems.Add(new HotSearchItem
                {
                    Index = index++,
                    Text = item.Title,
                    Keyword = item.Title,
                    Tag = item.Tag,
                    Heat = heat,
                    Icon = icon,
                });
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
