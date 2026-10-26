using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages.Overlay;
using PhoenixReadCopilot.UI.Strings;
using PhoenixReadCopilot.UI.Toolkits;
using PhoenixReadCopilot.UI.ViewModels.Components;
using PhoenixReadCopilot.UI.ViewModels.Core;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Search;
using Richasy.BiliKernel.Models.Media;
using Richasy.BiliKernel.Models.Search;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 搜索页面视图模型.
/// </summary>
public sealed partial class SearchPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchPageViewModel"/> class.
    /// </summary>
    public SearchPageViewModel(ISearchService service, ILogger<SearchPageViewModel> logger)
    {
        _service = service;
        _logger = logger;
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
            var videoId = keyword[2..];

            // videoId should be a number.
            if (long.TryParse(videoId, out var id))
            {
                this.Get<NavigationViewModel>().Back();
                var video = new VideoInformation(
                    new MediaIdentifier(videoId, default, default),
                    default
                );
                new VideoItemViewModel(video, VideoCardStyle.Search).PlayCommand.Execute(default);
                return;
            }
        }
        else if (keyword.StartsWith("BV"))
        {
            try
            {
                var video = new VideoInformation(
                    new MediaIdentifier(IdToolkit.Bv2Av(keyword).ToString(), default, default),
                    default
                );
                this.Get<NavigationViewModel>().Back();
                new VideoItemViewModel(video, VideoCardStyle.Search).PlayCommand.Execute(default);
                return;
            }
            catch (Exception)
            {
                Debug.WriteLine("尝试解析BV号时失败.");
            }
        }

        Keyword = keyword;
        Sections = default;
        SelectedSection = default;

        try
        {
            IsSearching = true;
            var (videos, nextVideoOffset) = await _service.GetComprehensiveSearchResultAsync(
                keyword
            );
            var sections = new List<ISearchSectionDetailViewModel>
            {
                CreateVideoSection(videos, nextVideoOffset),
                CreateSection(SearchSectionType.Anime),
                CreateSection(SearchSectionType.Cinema),
                CreateSection(SearchSectionType.User),
                CreateSection(SearchSectionType.Article),
            };
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
        section.TryFirstLoadCommand.Execute(default);
    }

    private VideoSearchSectionDetailViewModel CreateVideoSection(
        IReadOnlyList<VideoInformation> videos,
        int? page
    )
    {
        var newSection = new VideoSearchSectionDetailViewModel(_service);
        newSection.Initialize(Keyword, new SearchPartition(0, Resources.Video));
        newSection.SetFirstPageData(videos, page);
        return newSection;
    }

    private ISearchSectionDetailViewModel CreateSection(SearchSectionType type)
    {
        var sectionDetail = type switch
        {
            SearchSectionType.Anime or SearchSectionType.Cinema => (ISearchSectionDetailViewModel)
                new PgcSearchSectionDetailViewModel(_service),
            SearchSectionType.User => new UserSearchSectionDetailViewModel(_service),
            SearchSectionType.Article => new ArticleSearchSectionDetailViewModel(_service),
            _ => default,
        };

        var partition = type switch
        {
            SearchSectionType.Anime => new SearchPartition(7, Resources.Anime),
            SearchSectionType.Cinema => new SearchPartition(8, Resources.Cinema),
            SearchSectionType.User => new SearchPartition(2, Resources.User),
            SearchSectionType.Article => new SearchPartition(6, Resources.Article),
            _ => default,
        };

        sectionDetail?.Initialize(Keyword, partition);
        return sectionDetail;
    }
}
