using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Helpers;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 播放历史记录服务实现.
/// </summary>
public sealed class HistoryService : IHistoryService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<HistoryService> _logger;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<ReadHistoryItem>? _cache;
    private string? _cachedFilePath;

    /// <inheritdoc/>
    public event EventHandler? HistoryChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryService"/> class.
    /// </summary>
    public HistoryService(
        PhoenixReadApiClient apiClient,
        IAuthenticationService authService,
        IPathService pathService,
        ILogger<HistoryService> logger
    )
    {
        _apiClient = apiClient;
        _authService = authService;
        _logger = logger;
        _folderPath = Path.Combine(pathService.DataFolderPath, "History");
        EnsureDirectory();

        _authService.StateChanged += OnAuthStateChanged;
    }

    private void EnsureDirectory()
    {
        try
        {
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"创建历史记录数据目录失败: {_folderPath}");
        }
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        _lock.Wait();
        try
        {
            _cache = null;
            _cachedFilePath = null;
        }
        finally
        {
            _lock.Release();
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetStorageFilePath()
    {
        string fileName =
            _authService.IsAuthenticated && !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
                ? $"{_authService.CurrentUser.Id}.json"
                : "anonymous.json";
        return Path.Combine(_folderPath, fileName);
    }

    /// <inheritdoc/>
    public Task ReportProgressAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        long currentPositionMs,
        long durationMs,
        string? title = null,
        string? coverUrl = null,
        CancellationToken cancellationToken = default
    ) =>
        ReportProgressAsync(
            new ProgressUpdate
            {
                DramaId = dramaId,
                VideoId = videoId,
                EpisodeNumber = episodeNumber,
                CurrentPositionMs = currentPositionMs,
                DurationMs = durationMs,
            },
            title,
            coverUrl,
            cancellationToken
        );

    /// <inheritdoc/>
    public async Task ReportProgressAsync(
        ProgressUpdate update,
        string? title = null,
        string? coverUrl = null,
        CancellationToken cancellationToken = default
    )
    {
        if (
            update is null
            || string.IsNullOrWhiteSpace(update.DramaId)
            || string.IsNullOrWhiteSpace(update.VideoId)
        )
        {
            return;
        }

        // 1. 发送到在线接口（未登录时匿名上报；发生网络异常不阻断本地存储）
        try
        {
            await _apiClient.ReportProgressAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"向服务端上报播放进度失败: DramaId={update.DramaId}, VideoId={update.VideoId}");
        }

        // 2. 本地持久化保存
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync();

            ReadHistoryItem? existing = _cache!.FirstOrDefault(x => x.DramaId == update.DramaId);

            string resolvedTitle = !string.IsNullOrWhiteSpace(title)
                ? title
                : (existing?.Title ?? string.Empty);

            string resolvedCoverUrl = !string.IsNullOrWhiteSpace(coverUrl)
                ? coverUrl
                : (existing?.CoverUrl ?? string.Empty);

            long timestamp =
                update.UpdatedAtUnixMilliseconds > 0
                    ? update.UpdatedAtUnixMilliseconds
                    : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            ReadHistoryItem newItem = new()
            {
                Id = existing?.Id ?? $"{update.DramaId}_{update.VideoId}",
                DramaId = update.DramaId,
                VideoId = update.VideoId,
                EpisodeNumber = update.EpisodeNumber > 0
                    ? update.EpisodeNumber
                    : (existing?.EpisodeNumber ?? 1),
                Title = resolvedTitle,
                CoverUrl = resolvedCoverUrl,
                CurrentPositionMs = Math.Max(0, update.CurrentPositionMs),
                DurationMs = Math.Max(
                    0,
                    update.DurationMs > 0 ? update.DurationMs : (existing?.DurationMs ?? 0)
                ),
                UpdatedAtUnixMilliseconds = timestamp,
            };

            if (existing is not null)
            {
                _cache!.Remove(existing);
            }

            _cache!.Insert(0, newItem);
            await SaveToFileAsync();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"本地保存播放进度记录失败: DramaId={update.DramaId}");
        }
        finally
        {
            _lock.Release();
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task<ReadHistoryPage> GetHistoryAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    )
    {
        // 登录状态优先从服务端获取
        if (_authService.IsAuthenticated)
        {
            try
            {
                ReadHistoryPage onlinePage = await _apiClient.GetHistoryAsync(
                    count: count,
                    offset: offset,
                    querySoftDeleted: false,
                    lastMinReadTimestampMilliseconds: 0,
                    cancellationToken: cancellationToken
                );

                // 可选：将第一页同步一份到本地缓存
                if (offset == 0 && onlinePage.Items.Count > 0)
                {
                    _ = SyncOnlineItemsToLocalCacheAsync(onlinePage.Items);
                }

                return onlinePage;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"从服务端获取阅读历史失败，将降级读取本地缓存.");
            }
        }

        // 未登录或服务端请求失败时，直接读取本地持久化数据
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync();
            int total = _cache!.Count;
            List<ReadHistoryItem> pagedItems = _cache!
                .Skip(offset)
                .Take(count)
                .ToList();

            return new ReadHistoryPage
            {
                Items = pagedItems,
                Total = total,
                NextOffset = offset + pagedItems.Count,
                HasMore = offset + pagedItems.Count < total,
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteHistoryAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(dramaId))
        {
            return;
        }

        if (_authService.IsAuthenticated)
        {
            try
            {
                await _apiClient.DeleteHistoryAsync(dramaId, videoId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"向服务端删除历史记录失败: DramaId={dramaId}, VideoId={videoId}");
            }
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync();
            int removed = _cache!.RemoveAll(x =>
                x.DramaId == dramaId
                && (string.IsNullOrWhiteSpace(videoId) || x.VideoId == videoId)
            );
            if (removed > 0)
            {
                await SaveToFileAsync();
            }
        }
        finally
        {
            _lock.Release();
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedAsync();
            _cache!.Clear();
            await SaveToFileAsync();
        }
        finally
        {
            _lock.Release();
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task SyncOnlineItemsToLocalCacheAsync(IReadOnlyList<ReadHistoryItem> items)
    {
        try
        {
            await _lock.WaitAsync();
            try
            {
                _cache = [.. items];
                _cachedFilePath = GetStorageFilePath();
                await SaveToFileAsync();
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"同步在线历史记录到本地失败.");
        }
    }

    private async Task EnsureLoadedAsync()
    {
        string currentFilePath = GetStorageFilePath();
        if (_cache is not null && _cachedFilePath == currentFilePath)
        {
            return;
        }

        _cachedFilePath = currentFilePath;

        if (File.Exists(currentFilePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(currentFilePath);
                _cache = JsonAotSerializer.Deserialize<List<ReadHistoryItem>>(json) ?? [];
                return;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"读取历史记录缓存文件失败，将重新初始化: {currentFilePath}");
            }
        }

        _cache = [];
    }

    private async Task SaveToFileAsync()
    {
        string targetFilePath = _cachedFilePath ?? GetStorageFilePath();
        try
        {
            string json = JsonAotSerializer.Serialize(_cache);
            await File.WriteAllTextAsync(targetFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"保存历史记录文件失败: {targetFilePath}");
        }
    }
}
