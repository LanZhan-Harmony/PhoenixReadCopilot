using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 首页视图模型.
/// </summary>
internal sealed partial class HomePageViewModel(
    IRecommendService recommendService,
    ILogger<HomePageViewModel> logger,
    INavigationService navigationService,
    DispatcherQueue dispatcherQueue,
    ISettingsService settingsService
) : LayoutPageViewModelBase(settingsService)
{
    private readonly IRecommendService _recommendService = recommendService;
    private readonly ILogger<HomePageViewModel> _logger = logger;
    private readonly INavigationService _navigationService = navigationService;
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;

    private int _recommendOffset;
    private string? _recommendSessionId;
    private readonly HashSet<string> _seenDramaIds = [];
    private bool _hasMoreRecommendations = true;

    private long _currentRequestId;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 是否正在加载推荐.
    /// </summary>
    [ObservableProperty]
    public partial bool IsVideoLoading { get; set; }

    /// <summary>
    /// 推荐视频列表已更新事件.
    /// </summary>
    public event EventHandler? VideoListUpdated;

    /// <summary>
    /// 推荐短剧列表.
    /// </summary>
    public ObservableCollection<DramaItemViewModel> Videos { get; } = [];

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(HomePage);

    public async Task InitializeAsync()
    {
        if (Videos.Count > 0)
        {
            return;
        }

        await LoadVideosAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        IsVideoLoading = false;

        Videos.Clear();
        _recommendOffset = 0;
        _recommendSessionId = null;
        _seenDramaIds.Clear();
        _hasMoreRecommendations = true;

        await LoadVideosAsync();
    }

    [RelayCommand]
    private void PlayCuratedList()
    {
        DramaItemViewModel? first = Videos.FirstOrDefault();
        if (first?.Drama is not null)
        {
            _navigationService.NavigateToOver(typeof(VideoPlayerPage), first.Drama);
        }
    }

    public async Task LoadVideosAsync()
    {
        if (IsVideoLoading)
        {
            return;
        }

        IsVideoLoading = true;
        long requestId = ++_currentRequestId;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        CancellationToken ct = _cancellationTokenSource.Token;

        try
        {
            await LoadRecommendVideosAsync(requestId, ct);
        }
        finally
        {
            if (requestId == _currentRequestId)
            {
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    IsVideoLoading = false
                );
            }
        }
    }

    private async Task LoadRecommendVideosAsync(long requestId, CancellationToken ct)
    {
        if (!_hasMoreRecommendations && _recommendOffset > 0)
        {
            return;
        }

        try
        {
            RecommendationPage page = await _recommendService.GetRecommendationsAsync(
                RecommendationGenre.ShortDrama,
                offset: _recommendOffset,
                sessionId: _recommendSessionId,
                seen: _seenDramaIds,
                cancellationToken: ct
            );

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            _recommendOffset = page.NextOffset;
            _recommendSessionId = page.SessionId;
            _hasMoreRecommendations = page.HasMore;

            if (page.Items is { Count: > 0 })
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (requestId != _currentRequestId || ct.IsCancellationRequested)
                    {
                        return;
                    }

                    int addedCount = 0;
                    foreach (Drama item in page.Items)
                    {
                        if (string.IsNullOrWhiteSpace(item.Id))
                        {
                            continue;
                        }

                        // 严格自动去重：不仅在当前推荐会话中去重，还要确保当前展示列表中绝不包含重复剧集
                        if (_seenDramaIds.Add(item.Id) && !Videos.Any(v => v.Drama?.Id == item.Id))
                        {
                            Videos.Add(new DramaItemViewModel(item, VideoCardStyle.Recommend));
                            addedCount++;
                        }
                    }

                    if (addedCount > 0)
                    {
                        VideoListUpdated?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            // 请求已取消，正常忽略
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载首页短剧推荐时出错.");
        }
    }
}
