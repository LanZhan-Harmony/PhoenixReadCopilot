using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 流行与榜单页面视图模型 (数据请求分部).
/// </summary>
public sealed partial class PopularPageViewModel
{
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

                    if (SelectedSection != null)
                    {
                        _videoCache[GetCacheKey(SelectedSection, CurrentChannel)] = Videos.ToList();
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

    private async Task LoadDramaLeaderboardAsync(
        ShortDramaLeaderboardType type,
        ShortDramaLeaderboardChannel channel,
        long requestId,
        CancellationToken ct
    )
    {
        if (!_hasMoreLeaderboard && _leaderboardOffset > 0)
        {
            return;
        }

        try
        {
            ShortDramaLeaderboardPage page = await _apiClient.GetShortDramaLeaderboardAsync(
                type: type,
                channel: channel,
                offset: _leaderboardOffset,
                sessionId: _leaderboardSessionId,
                cancellationToken: ct
            );

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            _leaderboardOffset = page.NextOffset;
            _leaderboardSessionId = page.SessionId;
            _hasMoreLeaderboard = page.HasMore;

            BoardTitle = page.BoardTitle;
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
                        if (
                            string.IsNullOrWhiteSpace(item.Id)
                            || Videos.Any(v => v.Drama?.Id == item.Id)
                        )
                        {
                            continue;
                        }

                        VideoItemViewModel vm = new(item, VideoCardStyle.Rank)
                        {
                            Rank = Videos.Count + 1,
                        };
                        Videos.Add(vm);
                    }

                    if (SelectedSection != null)
                    {
                        _videoCache[GetCacheKey(SelectedSection, CurrentChannel)] = Videos.ToList();
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "尝试加载红果短剧排行榜时出错. Type: {Type}, Channel: {Channel}",
                type,
                channel
            );
        }
    }

    private async Task LoadActorLeaderboardAsync(long requestId, CancellationToken ct)
    {
        if (!_hasMoreActor && _actorOffset > 0)
        {
            return;
        }

        try
        {
            ActorLeaderboardPage page = await _apiClient.GetActorLeaderboardAsync(
                offset: _actorOffset,
                cancellationToken: ct
            );

            if (requestId != _currentRequestId || ct.IsCancellationRequested)
            {
                return;
            }

            _actorOffset = page.NextOffset;
            _actorSessionId = page.SessionId;
            _hasMoreActor = page.HasMore;

            if (page.Items is { Count: > 0 })
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (requestId != _currentRequestId || ct.IsCancellationRequested)
                    {
                        return;
                    }

                    foreach (ActorLeaderboardItem item in page.Items)
                    {
                        string actorKey = !string.IsNullOrEmpty(item.ActorId)
                            ? item.ActorId
                            : item.UserId;
                        if (
                            string.IsNullOrWhiteSpace(actorKey)
                            || Videos.Any(v => v.ActorId == actorKey)
                        )
                        {
                            continue;
                        }

                        VideoItemViewModel vm = new(item)
                        {
                            Rank = item.Position > 0 ? item.Position : Videos.Count + 1,
                        };
                        Videos.Add(vm);
                    }

                    if (SelectedSection != null)
                    {
                        _videoCache[GetCacheKey(SelectedSection, CurrentChannel)] = Videos.ToList();
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试加载红果演员排行榜时出错.");
        }
    }
}
