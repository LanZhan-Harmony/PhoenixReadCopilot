using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Helpers;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 播放/阅读历史记录服务实现（支持短剧，并预留视频、图文、书籍等扩展，支持登录与未登录隔离分文件存储）.
/// </summary>
public sealed class HistoryService : IHistoryService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<HistoryService> _logger;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<ReadHistoryItem>? _dramaCache;
    private string? _cachedUserKey;

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
        _folderPath = pathService.HistoryFolderPath;

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
            _dramaCache = null;
            _cachedUserKey = null;
        }
        finally
        {
            _lock.Release();
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetStorageFilePath(HistoryType type)
    {
        string userFolder =
            _authService.IsAuthenticated && !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
                ? _authService.CurrentUser.Id
                : "anonymous";
        string targetDir = Path.Combine(_folderPath, userFolder);
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        string fileName = type switch
        {
            HistoryType.Drama => "drama.json",
            HistoryType.Video => "video.json",
            HistoryType.Post => "post.json",
            HistoryType.Novel => "novel.json",
            _ => "drama.json",
        };
        return Path.Combine(targetDir, fileName);
    }

    private async Task EnsureLoadedLockedAsync(HistoryType type = HistoryType.Drama)
    {
        string userFolder =
            _authService.IsAuthenticated && !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
                ? _authService.CurrentUser.Id
                : "anonymous";

        if (type == HistoryType.Drama)
        {
            if (_dramaCache != null && _cachedUserKey == userFolder)
            {
                return;
            }

            string filePath = GetStorageFilePath(HistoryType.Drama);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    List<ReadHistoryItem>? items = JsonAotSerializer.Deserialize<
                        List<ReadHistoryItem>
                    >(json);
                    _dramaCache = items ?? [];
                }
                catch (Exception ex)
                {
                    _logger.ZLogWarning(ex, $"加载本地历史记录失败: {filePath}");
                    _dramaCache = [];
                }
            }
            else
            {
                _dramaCache = [];
            }

            _cachedUserKey = userFolder;
        }
    }

    private async Task SaveCacheLockedAsync(HistoryType type = HistoryType.Drama)
    {
        try
        {
            if (type == HistoryType.Drama)
            {
                string filePath = GetStorageFilePath(HistoryType.Drama);
                string json = JsonAotSerializer.Serialize(_dramaCache ?? []);
                await File.WriteAllTextAsync(filePath, json);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"持久化历史记录数据失败: 类型={type}");
        }
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
        HistoryType type = HistoryType.Drama,
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
            type,
            cancellationToken
        );

    /// <inheritdoc/>
    public async Task ReportProgressAsync(
        ProgressUpdate update,
        string? title = null,
        string? coverUrl = null,
        HistoryType type = HistoryType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (type != HistoryType.Drama)
        {
            _logger.ZLogInformation($"图文或书籍暂无在线进度上报支持，仅记录操作: 类型={type}");
            return;
        }

        if (string.IsNullOrEmpty(update.DramaId))
        {
            return;
        }

        // 1. 若已登录，尝试在线上报
        if (_authService.IsAuthenticated)
        {
            try
            {
                await _apiClient.ReportProgressAsync(update, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.ZLogWarning(ex, $"在线上报播放进度失败，保存至本地缓存: {update.DramaId}");
            }
        }
        else
        {
            // 未登录模式：尝试在线请求（若支持），但不阻塞本地保存
            try
            {
                await _apiClient.ReportProgressAsync(update, cancellationToken);
            }
            catch
            {
                // 匿名状态服务端可能拒绝，忽略并保存到本地
            }
        }

        // 2. 本地持久化保存
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync(HistoryType.Drama);

            int index = _dramaCache!.FindIndex(p => p.DramaId == update.DramaId);
            ReadHistoryItem item;
            if (index >= 0)
            {
                ReadHistoryItem old = _dramaCache[index];
                _dramaCache.RemoveAt(index);
                item = new ReadHistoryItem
                {
                    DramaId = update.DramaId,
                    VideoId = update.VideoId,
                    Title = !string.IsNullOrEmpty(title) ? title : old.Title,
                    CoverUrl = !string.IsNullOrEmpty(coverUrl) ? coverUrl : old.CoverUrl,
                    EpisodeNumber =
                        update.EpisodeNumber > 0 ? update.EpisodeNumber : old.EpisodeNumber,
                    CurrentPositionMs = update.CurrentPositionMs,
                    DurationMs = update.DurationMs > 0 ? update.DurationMs : old.DurationMs,
                    UpdatedAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };
            }
            else
            {
                item = new ReadHistoryItem
                {
                    DramaId = update.DramaId,
                    VideoId = update.VideoId,
                    Title = title ?? string.Empty,
                    CoverUrl = coverUrl ?? string.Empty,
                    EpisodeNumber = update.EpisodeNumber,
                    CurrentPositionMs = update.CurrentPositionMs,
                    DurationMs = update.DurationMs,
                    UpdatedAtUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };
            }

            _dramaCache.Insert(0, item);
            await SaveCacheLockedAsync(HistoryType.Drama);
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
        HistoryType type = HistoryType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (type != HistoryType.Drama)
        {
            return new ReadHistoryPage
            {
                Items = [],
                Total = 0,
                NextOffset = 0,
                HasMore = false,
            };
        }

        // 1. 已登录模式优先在线拉取
        if (_authService.IsAuthenticated)
        {
            try
            {
                ReadHistoryPage onlinePage = await _apiClient.GetHistoryAsync(
                    count: count,
                    offset: offset,
                    cancellationToken: cancellationToken
                );

                await _lock.WaitAsync(cancellationToken);
                try
                {
                    await EnsureLoadedLockedAsync(HistoryType.Drama);
                    if (onlinePage.Items is { Count: > 0 } items)
                    {
                        foreach (ReadHistoryItem onlineItem in items)
                        {
                            if (string.IsNullOrEmpty(onlineItem.DramaId))
                            {
                                continue;
                            }

                            int idx = _dramaCache!.FindIndex(p => p.DramaId == onlineItem.DramaId);
                            if (idx >= 0)
                            {
                                ReadHistoryItem local = _dramaCache[idx];
                                _dramaCache[idx] = new ReadHistoryItem
                                {
                                    DramaId = onlineItem.DramaId,
                                    VideoId = !string.IsNullOrEmpty(onlineItem.VideoId)
                                        ? onlineItem.VideoId
                                        : local.VideoId,
                                    Title = !string.IsNullOrEmpty(onlineItem.Title)
                                        ? onlineItem.Title
                                        : local.Title,
                                    CoverUrl = !string.IsNullOrEmpty(onlineItem.CoverUrl)
                                        ? onlineItem.CoverUrl
                                        : local.CoverUrl,
                                    EpisodeNumber =
                                        onlineItem.EpisodeNumber > 0
                                            ? onlineItem.EpisodeNumber
                                            : local.EpisodeNumber,
                                    CurrentPositionMs =
                                        onlineItem.CurrentPositionMs > 0
                                            ? onlineItem.CurrentPositionMs
                                            : local.CurrentPositionMs,
                                    DurationMs =
                                        onlineItem.DurationMs > 0
                                            ? onlineItem.DurationMs
                                            : local.DurationMs,
                                    UpdatedAtUnixMilliseconds =
                                        onlineItem.UpdatedAtUnixMilliseconds > 0
                                            ? onlineItem.UpdatedAtUnixMilliseconds
                                            : local.UpdatedAtUnixMilliseconds,
                                };
                            }
                            else
                            {
                                _dramaCache.Add(onlineItem);
                            }
                        }

                        await SaveCacheLockedAsync(HistoryType.Drama);
                    }
                }
                finally
                {
                    _lock.Release();
                }

                return onlinePage;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.ZLogWarning(ex, $"获取在线播放历史失败，降级使用本地历史记录.");
            }
        }

        // 2. 未登录模式或在线降级：使用本地历史
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync(HistoryType.Drama);

            int safeCount = Math.Max(1, count);
            int safeOffset = Math.Max(0, offset);
            List<ReadHistoryItem> slice = _dramaCache!.Skip(safeOffset).Take(safeCount).ToList();
            return new ReadHistoryPage
            {
                Items = slice,
                Total = _dramaCache.Count,
                NextOffset = safeOffset + slice.Count,
                HasMore = safeOffset + slice.Count < _dramaCache.Count,
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
        HistoryType type = HistoryType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (type != HistoryType.Drama || string.IsNullOrEmpty(dramaId))
        {
            return;
        }

        if (_authService.IsAuthenticated)
        {
            try
            {
                await _apiClient.DeleteHistoryAsync(dramaId, videoId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.ZLogWarning(ex, $"在线删除历史记录失败，仍同步从本地移除: {dramaId}");
            }
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync(HistoryType.Drama);
            _dramaCache!.RemoveAll(p => p.DramaId == dramaId);
            await SaveCacheLockedAsync(HistoryType.Drama);
        }
        finally
        {
            _lock.Release();
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task ClearHistoryAsync(
        HistoryType type = HistoryType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (type != HistoryType.Drama)
        {
            return;
        }

        if (_authService.IsAuthenticated && _dramaCache is { Count: > 0 } items)
        {
            foreach (ReadHistoryItem item in items.ToList())
            {
                try
                {
                    await _apiClient.DeleteHistoryAsync(
                        item.DramaId,
                        item.VideoId,
                        cancellationToken
                    );
                }
                catch
                {
                    // 忽略单个删除异常
                }
            }
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync(HistoryType.Drama);
            _dramaCache!.Clear();
            await SaveCacheLockedAsync(HistoryType.Drama);
        }
        finally
        {
            _lock.Release();
        }

        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
