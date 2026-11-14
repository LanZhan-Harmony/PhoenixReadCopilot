using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 首页视图模型 (请求分部).
/// </summary>
public sealed partial class HomePageViewModel
{
    [RelayCommand]
    private async Task LoadVideosAsync()
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
                VideoListUpdated?.Invoke(this, EventArgs.Empty);
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
            RecommendationPage page = await _apiClient.GetRecommendationsAsync(
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

                    foreach (Drama item in page.Items)
                    {
                        if (string.IsNullOrWhiteSpace(item.Id))
                        {
                            continue;
                        }

                        // 严格自动去重：不仅在当前推荐会话中去重，还要确保当前展示列表中绝不包含重复剧集
                        if (_seenDramaIds.Add(item.Id) && !Videos.Any(v => v.Drama?.Id == item.Id))
                        {
                            Videos.Add(new VideoItemViewModel(item, VideoCardStyle.Recommend));
                        }
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
