using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 流行与榜单页面视图模型 (数据请求分部).
/// </summary>
public sealed partial class PopularPageViewModel
{
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

                    if (SelectedSection is not null)
                    {
                        _videoCache[GetCacheKey(SelectedSection, CurrentChannel)] = [.. Videos];
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"尝试加载红果短剧排行榜时出错. Type: {type}, Channel: {channel}"
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
                            || Actors.Any(v => v.ActorId == actorKey)
                        )
                        {
                            continue;
                        }

                        ActorItemViewModel vm = new(item)
                        {
                            Rank = item.Position > 0 ? item.Position : Actors.Count + 1,
                        };
                        Actors.Add(vm);
                    }

                    if (SelectedSection is not null)
                    {
                        _actorCache[GetCacheKey(SelectedSection, CurrentChannel)] = [.. Actors];
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载红果演员排行榜时出错.");
        }
    }
}
