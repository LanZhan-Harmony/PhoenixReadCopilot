using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Search;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Components;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 搜索页面视图模型.
/// </summary>
internal sealed partial class SearchPageViewModel(
    ISearchService searchService,
    ILogger<SearchPageViewModel> logger,
    INavigationService navigationService,
    ISettingsService settingsService,
    DispatcherQueue dispatcherQueue,
    ILoggerFactory loggerFactory
) : LayoutPageViewModelBase(settingsService)
{
    private readonly ISearchService _searchService = searchService;
    private readonly ILogger<SearchPageViewModel> _logger = logger;
    private readonly INavigationService _navigationService = navigationService;
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    [ObservableProperty]
    public partial string Keyword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial ISearchSectionDetailViewModel? SelectedSection { get; set; }

    /// <summary>
    /// 详情分区列表.
    /// </summary>
    [ObservableProperty]
    public partial List<ISearchSectionDetailViewModel>? Sections { get; set; }

    /// <summary>
    /// 分区初始化完成.
    /// </summary>
    public event EventHandler? SectionInitialized;

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(SearchPage);

    public void Search(string keyword) => _ = SearchAsync(keyword);

    public async Task SearchAsync(string keyword)
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
            SearchResult searchResult = await _searchService.SearchAsync(keyword);

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
    public void SelectSection(ISearchSectionDetailViewModel? section)
    {
        if (section is null || section == SelectedSection)
        {
            return;
        }

        SelectedSection = section;
        _ = section.TryFirstLoadAsync();
    }

    private VideoSearchSectionDetailViewModel CreateVideoSection(
        IReadOnlyList<Drama>? dramas,
        int? page
    )
    {
        VideoSearchSectionDetailViewModel newSection = new(
            _searchService,
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
            _searchService,
            _dispatcherQueue,
            _loggerFactory.CreateLogger<UserSearchSectionDetailViewModel>()
        );
        userSection.Initialize(Keyword, new SearchPartition(2, Resources.User));
        return userSection;
    }
}
