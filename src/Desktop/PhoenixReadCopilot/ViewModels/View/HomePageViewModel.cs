using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 首页视图模型.
/// </summary>
internal sealed partial class HomePageViewModel : LayoutPageViewModelBase
{
    private const int CarouselLimit = 10;

    private readonly IRecommendService _recommendService;
    private readonly ILogger<HomePageViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    private readonly HomeSectionItemViewModel _recommendSection;
    private readonly HomeSectionItemViewModel _newArrivalSection;

    private string? _discoverCursor;
    private bool _hasMoreDiscover = true;
    private readonly HashSet<string> _seenRecommendIds = [];

    private int _newArrivalOffset;
    private string? _newArrivalSessionId;
    private bool _hasMoreNewArrivals = true;
    private readonly HashSet<string> _seenNewArrivalIds = [];

    private bool _hasLoadedCarousel;
    private long _currentRequestId;
    private CancellationTokenSource? _videosCts;
    private CancellationTokenSource? _carouselCts;

    /// <summary>
    /// 是否正在加载短剧列表.
    /// </summary>
    [ObservableProperty]
    public partial bool IsVideoLoading { get; set; }

    /// <summary>
    /// 当前选中的首页分区.
    /// </summary>
    [ObservableProperty]
    public partial HomeSectionItemViewModel? SelectedSection { get; set; }

    /// <summary>
    /// 顶部轮播新剧列表（最多 10 条）.
    /// </summary>
    [ObservableProperty]
    public partial IReadOnlyList<DramaItemViewModel> CarouselDramas { get; set; } = [];

    /// <summary>
    /// 当前分区是否仍有更多数据可加载.
    /// </summary>
    public bool HasMoreCurrentVideos =>
        SelectedSection?.SectionType == HomeSectionType.NewArrival
            ? _hasMoreNewArrivals
            : _hasMoreDiscover;

    /// <summary>
    /// 当前分区是否已有数据.
    /// </summary>
    public bool HasCurrentVideos => CurrentVideos.Count > 0;

    /// <summary>
    /// 短剧列表已更新事件.
    /// </summary>
    public event EventHandler? VideoListUpdated;

    /// <summary>
    /// 推荐短剧列表.
    /// </summary>
    public ObservableCollection<DramaItemViewModel> RecommendVideos { get; } = [];

    /// <summary>
    /// 上新短剧列表.
    /// </summary>
    public ObservableCollection<DramaItemViewModel> NewArrivalVideos { get; } = [];

    private ObservableCollection<DramaItemViewModel> CurrentVideos =>
        SelectedSection?.SectionType == HomeSectionType.NewArrival
            ? NewArrivalVideos
            : RecommendVideos;

    public HomePageViewModel(
        IRecommendService recommendService,
        ILogger<HomePageViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _recommendService = recommendService;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;

        _recommendSection = new HomeSectionItemViewModel(
            HomeSectionType.Recommend,
            Resources.Recommend,
            this
        );
        _newArrivalSection = new HomeSectionItemViewModel(
            HomeSectionType.NewArrival,
            Resources.NewArrival,
            this
        );
        SelectedSection = _recommendSection;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(HomePage);

    /// <summary>
    /// 初始化首页轮播与当前选中分区数据.
    /// </summary>
    public async Task InitializeAsync()
    {
        List<Task> tasks = [];

        if (!_hasLoadedCarousel && CarouselDramas.Count == 0)
        {
            tasks.Add(LoadCarouselAsync());
        }

        if (CurrentVideos.Count == 0 && HasMoreCurrentVideos)
        {
            tasks.Add(LoadVideosAsync());
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 切换首页展示分区（推荐 / 上新）.
    /// </summary>
    public async Task SelectSectionAsync(HomeSectionType sectionType)
    {
        if (SelectedSection?.SectionType == sectionType)
        {
            return;
        }

        _videosCts?.Cancel();
        _videosCts?.Dispose();
        _videosCts = null;
        ++_currentRequestId;
        IsVideoLoading = false;

        SelectedSection =
            sectionType == HomeSectionType.NewArrival ? _newArrivalSection : _recommendSection;

        if (CurrentVideos.Count == 0 && HasMoreCurrentVideos)
        {
            await LoadVideosAsync();
        }
        else
        {
            VideoListUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _videosCts?.Cancel();
        _videosCts?.Dispose();
        _videosCts = null;
        ++_currentRequestId;
        IsVideoLoading = false;

        if (SelectedSection?.SectionType == HomeSectionType.NewArrival)
        {
            NewArrivalVideos.Clear();
            _newArrivalOffset = 0;
            _newArrivalSessionId = null;
            _seenNewArrivalIds.Clear();
            _hasMoreNewArrivals = true;
            _hasLoadedCarousel = false;
        }
        else
        {
            RecommendVideos.Clear();
            _discoverCursor = null;
            _seenRecommendIds.Clear();
            _hasMoreDiscover = true;
        }

        await LoadVideosAsync();
    }

    [RelayCommand]
    private void PlayCuratedList()
    {
        DramaItemViewModel? first = CurrentVideos.FirstOrDefault();
        first?.Play();
    }

    /// <summary>
    /// 加载当前分区的下一页短剧数据.
    /// </summary>
    public async Task LoadVideosAsync()
    {
        if (IsVideoLoading || !HasMoreCurrentVideos)
        {
            return;
        }

        IsVideoLoading = true;
        long requestId = ++_currentRequestId;
        _videosCts?.Cancel();
        _videosCts?.Dispose();
        _videosCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        CancellationToken ct = _videosCts.Token;
        HomeSectionType targetSection =
            SelectedSection?.SectionType ?? HomeSectionType.Recommend;

        try
        {
            if (targetSection == HomeSectionType.NewArrival)
            {
                await LoadNewArrivalVideosAsync(requestId, ct);
            }
            else
            {
                await LoadDiscoverVideosAsync(requestId, ct);
            }
        }
        finally
        {
            if (requestId == _currentRequestId)
            {
                _dispatcherQueue.TryEnqueue(
                    DispatcherQueuePriority.Low,
                    () =>
                    {
                        if (requestId == _currentRequestId)
                        {
                            IsVideoLoading = false;
                        }
                    }
                );
            }
        }
    }

    private async Task LoadCarouselAsync()
    {
        _carouselCts?.Cancel();
        _carouselCts?.Dispose();
        _carouselCts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        CancellationToken ct = _carouselCts.Token;

        try
        {
            NewDramaRecommendationPage page =
                await _recommendService.GetNewDramaRecommendationsAsync(
                    offset: 0,
                    cancellationToken: ct
                );

            if (ct.IsCancellationRequested)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                CarouselDramas = CreateCarouselItems(page.Items);
                _hasLoadedCarousel = true;

                // 若上新列表尚未自行加载，则复用首屏新剧推荐数据避免重复请求
                if (NewArrivalVideos.Count == 0 && _newArrivalOffset == 0)
                {
                    ApplyNewArrivalPage(page, requestOffset: 0);
                    if (SelectedSection?.SectionType == HomeSectionType.NewArrival)
                    {
                        VideoListUpdated?.Invoke(this, EventArgs.Empty);
                    }
                }
            });
        }
        catch (OperationCanceledException)
        {
            // 请求已取消，正常忽略
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载首页新剧轮播时出错.");
        }
    }

    private async Task LoadDiscoverVideosAsync(long requestId, CancellationToken ct)
    {
        if (!_hasMoreDiscover)
        {
            return;
        }

        try
        {
            DiscoverPage page = await _recommendService.GetDiscoverPageAsync(
                DiscoverChannel.Recommend,
                cursor: _discoverCursor,
                cancellationToken: ct
            );

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (requestId != _currentRequestId || ct.IsCancellationRequested)
                {
                    return;
                }

                _discoverCursor = page.Cursor;
                _hasMoreDiscover = page.HasMore && !string.IsNullOrWhiteSpace(page.Cursor);

                int addedCount = 0;
                foreach (Drama item in page.Dramas)
                {
                    if (string.IsNullOrWhiteSpace(item.Id))
                    {
                        continue;
                    }

                    if (
                        _seenRecommendIds.Add(item.Id)
                        && !RecommendVideos.Any(v => v.Drama.Id == item.Id)
                    )
                    {
                        RecommendVideos.Add(new DramaItemViewModel(item, VideoCardStyle.Recommend));
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    VideoListUpdated?.Invoke(this, EventArgs.Empty);
                }
            });
        }
        catch (OperationCanceledException)
        {
            // 请求已取消，正常忽略
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载首页发现推荐短剧时出错.");
        }
    }

    private async Task LoadNewArrivalVideosAsync(long requestId, CancellationToken ct)
    {
        if (!_hasMoreNewArrivals)
        {
            return;
        }

        try
        {
            int requestOffset = _newArrivalOffset;
            NewDramaRecommendationPage page =
                await _recommendService.GetNewDramaRecommendationsAsync(
                    offset: requestOffset,
                    sessionId: _newArrivalSessionId,
                    cancellationToken: ct
                );

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (requestId != _currentRequestId || ct.IsCancellationRequested)
                {
                    return;
                }

                if (requestOffset == 0 && (!_hasLoadedCarousel || CarouselDramas.Count == 0))
                {
                    CarouselDramas = CreateCarouselItems(page.Items);
                    _hasLoadedCarousel = true;
                }

                int addedCount = ApplyNewArrivalPage(page, requestOffset);
                if (addedCount > 0)
                {
                    VideoListUpdated?.Invoke(this, EventArgs.Empty);
                }
            });
        }
        catch (OperationCanceledException)
        {
            // 请求已取消，正常忽略
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载首页上新短剧时出错.");
        }
    }

    private int ApplyNewArrivalPage(NewDramaRecommendationPage page, int requestOffset)
    {
        bool hasValidNextOffset = page.NextOffset > requestOffset;
        _newArrivalOffset = page.NextOffset;
        _newArrivalSessionId = page.SessionId;
        _hasMoreNewArrivals = page.HasMore && hasValidNextOffset && page.Items.Count > 0;

        int addedCount = 0;
        foreach (Drama item in page.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                continue;
            }

            if (
                _seenNewArrivalIds.Add(item.Id)
                && !NewArrivalVideos.Any(v => v.Drama.Id == item.Id)
            )
            {
                NewArrivalVideos.Add(new DramaItemViewModel(item, VideoCardStyle.NewArrival));
                addedCount++;
            }
        }

        return addedCount;
    }

    private static IReadOnlyList<DramaItemViewModel> CreateCarouselItems(
        IReadOnlyList<Drama> items
    )
    {
        List<DramaItemViewModel> result = [];
        foreach (Drama item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                continue;
            }

            result.Add(new DramaItemViewModel(item, VideoCardStyle.NewArrival));
            if (result.Count >= CarouselLimit)
            {
                break;
            }
        }

        return result;
    }
}
