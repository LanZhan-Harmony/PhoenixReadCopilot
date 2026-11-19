using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Helpers;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 弹幕服务实现，负责分集弹幕拉取、本地持久化缓存、弹幕发送与互动操作.
/// </summary>
public sealed class DanmakuService : IDanmakuService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IPathService _pathService;
    private readonly ILogger<DanmakuService> _logger;
    private readonly string _folderPath;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, DanmakuPage> _memoryCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DanmakuService"/> class.
    /// </summary>
    public DanmakuService(
        PhoenixReadApiClient apiClient,
        IPathService pathService,
        ILogger<DanmakuService> logger
    )
    {
        _apiClient = apiClient;
        _pathService = pathService;
        _logger = logger;
        _folderPath = Path.Combine(pathService.DataFolderPath, "Danmaku");

        EnsureDirectory();
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
            _logger.ZLogError(ex, $"创建弹幕缓存主目录失败: {_folderPath}");
        }
    }

    private SemaphoreSlim GetLock(string cacheKey) =>
        _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

    private static string GetCacheKey(string dramaId, string videoId) => $"{dramaId}_{videoId}";

    private string GetDramaFolderPath(string dramaId) => Path.Combine(_folderPath, dramaId);

    private string GetVideoFilePath(string dramaId, string videoId) =>
        Path.Combine(GetDramaFolderPath(dramaId), $"{videoId}.json");

    /// <inheritdoc/>
    public async Task<DanmakuPage> GetDanmakuAsync(
        string dramaId,
        string videoId,
        long startMs = 0,
        long durationMs = 3600000,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        string cacheKey = GetCacheKey(dramaId, videoId);

        // 1. 内存缓存命中
        if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out DanmakuPage? memoryPage))
        {
            return memoryPage;
        }

        SemaphoreSlim semaphore = GetLock(cacheKey);
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            // 2. 双重检查内存缓存
            if (!forceRefresh && _memoryCache.TryGetValue(cacheKey, out DanmakuPage? cachedPage))
            {
                return cachedPage;
            }

            // 3. 检查磁盘文件缓存
            string filePath = GetVideoFilePath(dramaId, videoId);
            if (!forceRefresh && File.Exists(filePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(filePath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        DanmakuPage? diskPage = JsonAotSerializer.Deserialize<DanmakuPage>(json);
                        if (diskPage is not null && diskPage.Items is { Count: > 0 })
                        {
                            _memoryCache[cacheKey] = diskPage;
                            return diskPage;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ZLogWarning(ex, $"读取磁盘弹幕缓存失败: {filePath}");
                }
            }

            // 4. 从服务端获取
            DanmakuPage networkPage = await _apiClient.GetDanmakuAsync(
                dramaId,
                videoId,
                startMs,
                durationMs,
                cancellationToken
            );

            // 5. 若包含弹幕数据，则写入持久化缓存与内存缓存
            if (networkPage.Items is { Count: > 0 })
            {
                _memoryCache[cacheKey] = networkPage;
                try
                {
                    string dramaDir = GetDramaFolderPath(dramaId);
                    if (!Directory.Exists(dramaDir))
                    {
                        Directory.CreateDirectory(dramaDir);
                    }

                    string json = JsonAotSerializer.Serialize(networkPage);
                    string tempPath = filePath + ".tmp";
                    await File.WriteAllTextAsync(tempPath, json, cancellationToken);
                    File.Move(tempPath, filePath, overwrite: true);
                }
                catch (Exception ex)
                {
                    _logger.ZLogWarning(ex, $"保存分集弹幕到磁盘失败: {filePath}");
                }
            }

            return networkPage;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public Task<PhoenixOperationResult> SendDanmakuAsync(
        string dramaId,
        string videoId,
        string text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return _apiClient.SendDanmakuAsync(dramaId, videoId, text.Trim(), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<PhoenixOperationResult> SetDanmakuLikeAsync(
        string commentId,
        string videoId,
        bool active = true,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        return _apiClient.SetDanmakuLikeAsync(commentId, videoId, active, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<PhoenixOperationResult> DeleteDanmakuAsync(
        string commentId,
        string videoId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        return _apiClient.DeleteDanmakuAsync(commentId, videoId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ClearDanmakuCacheAsync(
        string dramaId,
        string? videoId = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);

        if (!string.IsNullOrWhiteSpace(videoId))
        {
            string cacheKey = GetCacheKey(dramaId, videoId);
            SemaphoreSlim semaphore = GetLock(cacheKey);
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                _memoryCache.TryRemove(cacheKey, out _);
                string filePath = GetVideoFilePath(dramaId, videoId);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        else
        {
            string dramaDir = GetDramaFolderPath(dramaId);
            foreach (string key in _memoryCache.Keys)
            {
                if (key.StartsWith($"{dramaId}_", StringComparison.OrdinalIgnoreCase))
                {
                    _memoryCache.TryRemove(key, out _);
                }
            }

            if (Directory.Exists(dramaDir))
            {
                try
                {
                    Directory.Delete(dramaDir, recursive: true);
                }
                catch (Exception ex)
                {
                    _logger.ZLogWarning(ex, $"删除短剧弹幕目录失败: {dramaDir}");
                }
            }
        }
    }
}
