using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.Toolkits;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Search;
using Richasy.BiliKernel.Models.Media;
using Richasy.BiliKernel.Models.Search;

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
        ISearchService service,
        ILogger<SearchPageViewModel> logger,
        NavigationViewModel navigationViewModel,
        ISettingsService settingsService,
        DispatcherQueue dispatcherQueue,
        ILoggerFactory loggerFactory
    )
        : base(settingsService)
    {
        _service = service;
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

        if (keyword == Keyword)
        {
            return;
        }

        if (keyword.StartsWith("av", StringComparison.InvariantCultureIgnoreCase))
        {
            string videoId = keyword[2..];

            // videoId should be a number.
            if (long.TryParse(videoId, out long id))
            {
                _navigationViewModel.Back();
                VideoInformation video = new(
                    new MediaIdentifier(videoId, null, null),
                    null
                );
                new VideoItemViewModel(video, VideoCardStyle.Search).PlayCommand.Execute(null);
                return;
            }
        }
        else if (keyword.StartsWith("BV"))
        {
            try
            {
                VideoInformation video = new(
                    new MediaIdentifier(IdToolkit.Bv2Av(keyword).ToString(), null, null),
                    null
                );
                _navigationViewModel.Back();
                new VideoItemViewModel(video, VideoCardStyle.Search).PlayCommand.Execute(null);
                return;
            }
            catch (Exception)
            {
                Debug.WriteLine("尝试解析BV号时失败.");
            }
        }

        Keyword = keyword;
        Sections = null;
        SelectedSection = null;

        try
        {
            IsSearching = true;
            (IReadOnlyList<VideoInformation>? videos, int? nextVideoOffset) =
                await _service.GetComprehensiveSearchResultAsync(keyword);
            List<ISearchSectionDetailViewModel> sections =
            [
                CreateVideoSection(videos, nextVideoOffset),
                CreateSection(SearchSectionType.Anime),
                CreateSection(SearchSectionType.Cinema),
                CreateSection(SearchSectionType.User),
                CreateSection(SearchSectionType.Article),
            ];
            Sections = [.. sections];
            SelectSection(Sections.FirstOrDefault());
            SectionInitialized?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试获取搜索结果时失败.");
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
    private void SelectSection(ISearchSectionDetailViewModel section)
    {
        if (section is null || section == SelectedSection)
        {
            return;
        }

        SelectedSection = section;
        section.TryFirstLoadCommand.Execute(null);
    }

    private VideoSearchSectionDetailViewModel CreateVideoSection(
        IReadOnlyList<VideoInformation> videos,
        int? page
    )
    {
        VideoSearchSectionDetailViewModel newSection = new(
            _service,
            _dispatcherQueue,
            _loggerFactory.CreateLogger<VideoSearchSectionDetailViewModel>()
        );
        newSection.Initialize(Keyword, new SearchPartition(0, Resources.Video));
        newSection.SetFirstPageData(videos, page);
        return newSection;
    }

    private ISearchSectionDetailViewModel CreateSection(SearchSectionType type)
    {
        ISearchSectionDetailViewModel? sectionDetail = type switch
        {
            SearchSectionType.Anime or SearchSectionType.Cinema => (ISearchSectionDetailViewModel)
                new PgcSearchSectionDetailViewModel(
                    _service,
                    _dispatcherQueue,
                    _loggerFactory.CreateLogger<PgcSearchSectionDetailViewModel>()
                ),
            SearchSectionType.User => new UserSearchSectionDetailViewModel(
                _service,
                _dispatcherQueue,
                _loggerFactory.CreateLogger<UserSearchSectionDetailViewModel>()
            ),
            SearchSectionType.Article => new ArticleSearchSectionDetailViewModel(
                _service,
                _dispatcherQueue,
                _loggerFactory.CreateLogger<ArticleSearchSectionDetailViewModel>()
            ),
            _ => null,
        };

        SearchPartition? partition = type switch
        {
            SearchSectionType.Anime => new SearchPartition(7, Resources.Anime),
            SearchSectionType.Cinema => new SearchPartition(8, Resources.Cinema),
            SearchSectionType.User => new SearchPartition(2, Resources.User),
            SearchSectionType.Article => new SearchPartition(6, Resources.Article),
            _ => null,
        };

        sectionDetail?.Initialize(Keyword, partition);
        return sectionDetail;
    }
}
