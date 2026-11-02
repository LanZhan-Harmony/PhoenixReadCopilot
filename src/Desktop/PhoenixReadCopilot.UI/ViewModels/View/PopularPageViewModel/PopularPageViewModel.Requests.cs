using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Media;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 流行视频页面视图模型.
/// </summary>
public sealed partial class PopularPageViewModel
{
    private async Task LoadRecommendVideosAsync()
    {
        if (!_hasMoreRecommendations && _recommendOffset > 0)
        {
            return;
        }

        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var ct = _cancellationTokenSource.Token;

            var page = await _apiClient.GetRecommendationsAsync(
                RecommendationGenre.ShortDrama,
                offset: _recommendOffset,
                sessionId: _recommendSessionId,
                seen: _seenDramaIds,
                cancellationToken: ct
            );

            _recommendOffset = page.NextOffset;
            _recommendSessionId = page.SessionId;
            _hasMoreRecommendations = page.HasMore;

            if (page.Items is { Count: > 0 })
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    foreach (var item in page.Items)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Id) && _seenDramaIds.Add(item.Id))
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
            _logger.LogError(ex, "尝试加载红果短剧推荐时出错.");
        }
    }

    private async Task LoadHotVideosAsync()
    {
        try
        {
            var (videos, nextOffset) = await _service.GetHotVideoListAsync(_hotOffset);
            _hotOffset = nextOffset;
            TryAddVideos(videos, VideoCardStyle.Hot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试加载热门视频时出错.");
        }
    }

    private async Task LoadTotalRankVideosAsync()
    {
        try
        {
            var videos = await _service.GetGlobalRankingListAsync();
            TryAddVideos(videos, VideoCardStyle.Rank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试加载全站排行榜视频时出错.");
        }
    }

    private async Task LoadPartitionRankVideosAsync(Partition partition)
    {
        try
        {
            var videos = await _service.GetPartitionRankingListAsync(partition);
            TryAddVideos(videos, VideoCardStyle.Rank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"尝试加载 {partition.Name} 分区排行榜视频时出错.");
        }
    }

    private async Task LoadPartitionsAsync()
    {
        IsPartitionLoading = true;

        try
        {
            var partitions = await _service.GetVideoPartitionsAsync();
            if (partitions != null)
            {
                // 去除资讯分区，因为资讯分区没有排行榜.
                foreach (var item in partitions.Where(p => p.Id != "202"))
                {
                    Sections.Add(new PopularRankPartitionViewModel(item));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试加载视频分区列表时出错.");
        }

        IsPartitionLoading = false;
    }

    private void TryAddVideos(IReadOnlyList<VideoInformation> videos, VideoCardStyle style)
    {
        if (videos is not null)
        {
            foreach (var item in videos)
            {
                Videos.Add(new VideoItemViewModel(item, style));
            }
        }
    }
}
