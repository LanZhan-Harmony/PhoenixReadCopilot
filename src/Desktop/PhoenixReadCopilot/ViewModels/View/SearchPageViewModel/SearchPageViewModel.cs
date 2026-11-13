using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Models.Search;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;
using PhoenixReadCopilot.Core.Contracts.Services;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 搜索页面视图模型.
/// </summary>
public sealed partial class SearchPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchPageViewModel"/> class.
    /// </summary>
    public SearchPageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<SearchPageViewModel> logger,
        NavigationViewModel navigationViewModel,
        ISettingsService settingsService,
        DispatcherQueue dispatcherQueue,
        ILoggerFactory loggerFactory
    )
        : base(settingsService)
    {
        _apiClient = apiClient;
        _logger = logger;
        _navigationViewModel = navigationViewModel;
        _dispatcherQueue = dispatcherQueue;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(SearchPage);

    [RelayCommand]
    private async Task SearchAsync(string keyword)
    {
        if (string.IsNullOrEmpty(keyword))
        {
            throw new ArgumentNullException(nameof(keyword));
        }

        if (keyword == Keyword && Sections is { Count: > 0 })
        {
            return;
        }

        Keyword = keyword;
        Sections = null;
        SelectedSection = null;

        try
        {
            IsSearching = true;
            SearchResult searchResult = await _apiClient.SearchAsync(keyword);

            VideoSearchSectionDetailViewModel videoSection = CreateVideoSection(
                searchResult.Dramas,
                searchResult.NextOffset
            );
            UserSearchSectionDetailViewModel userSection = CreateUserSection();

            List<ISearchSectionDetailViewModel> sections = [videoSection, userSection];
            Sections = [.. sections];
            SelectSection(Sections.FirstOrDefault());
            SectionInitialized?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试获取搜索结果时失败.");
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// 选择分区.
    /// </summary>
    [RelayCommand]
    private void SelectSection(ISearchSectionDetailViewModel? section)
    {
        if (section is null || section == SelectedSection)
        {
            return;
        }

        SelectedSection = section;
        section.TryFirstLoadCommand.Execute(null);
    }

    private VideoSearchSectionDetailViewModel CreateVideoSection(
        IReadOnlyList<Drama>? dramas,
        int? page
    )
    {
        VideoSearchSectionDetailViewModel newSection = new(
            _apiClient,
            _dispatcherQueue,
            _loggerFactory.CreateLogger<VideoSearchSectionDetailViewModel>()
        );
        newSection.Initialize(Keyword, new SearchPartition(0, Resources.Video));
        newSection.SetFirstPageData(dramas, page);
        return newSection;
    }

    private UserSearchSectionDetailViewModel CreateUserSection()
    {
        UserSearchSectionDetailViewModel userSection = new(
            _apiClient,
            _dispatcherQueue,
            _loggerFactory.CreateLogger<UserSearchSectionDetailViewModel>()
        );
        userSection.Initialize(Keyword, new SearchPartition(2, Resources.User));
        return userSection;
    }
}
