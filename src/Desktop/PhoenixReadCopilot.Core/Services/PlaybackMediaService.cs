using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Helpers;
using PhoenixReadCopilot.Core.Models.Playback;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 播放媒体源服务实现（提供短剧分集地址解析、规范化画质过滤与单剧单文件持久化缓存）.
/// </summary>
public sealed class PlaybackMediaService : IPlaybackMediaService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IPathService _pathService;
    private readonly ILogger<PlaybackMediaService> _logger;
    private readonly string _folderPath;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, DramaPlaybackMediaCache> _memoryCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackMediaService"/> class.
    /// </summary>
    public PlaybackMediaService(
        PhoenixReadApiClient apiClient,
        IPathService pathService,
        ILogger<PlaybackMediaService> logger
    )
    {
        _apiClient = apiClient;
        _pathService = pathService;
        _logger = logger;
        _folderPath = Path.Combine(pathService.DataFolderPath, "PlaybackMedia");
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
            _logger.ZLogError(ex, $"创建播放媒体缓存目录失败: {_folderPath}");
        }
    }

    private SemaphoreSlim GetLock(string dramaId) =>
        _locks.GetOrAdd(dramaId, _ => new SemaphoreSlim(1, 1));

    private string GetDramaFilePath(string dramaId) =>
        Path.Combine(_folderPath, $"{dramaId}.json");

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PlaybackMedia>> ResolveMediaAsync(
        string dramaId,
        string videoId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(dramaId) || string.IsNullOrWhiteSpace(videoId))
        {
            return [];
        }

        SemaphoreSlim gate = GetLock(dramaId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            DramaPlaybackMediaCache dramaCache = await LoadDramaCacheAsync(dramaId);

            // 1. 若非强制刷新，优先检查缓存中的媒体地址及其有效性
            if (!forceRefresh && dramaCache.Episodes.TryGetValue(videoId, out List<PlaybackMedia>? cachedList))
            {
                if (IsMediaListValidAndNotExpired(cachedList))
                {
                    _logger.ZLogDebug($"命中文档本地媒体缓存: DramaId={dramaId}, VideoId={videoId}, 画质数={cachedList.Count}");
                    return cachedList;
                }
                else
                {
                    _logger.ZLogInformation($"分集媒体地址已过期或失效，准备向服务端刷新: DramaId={dramaId}, VideoId={videoId}");
                }
            }

            // 2. 向服务端请求解析分集播放媒体源
            IReadOnlyList<PlaybackMedia> freshMedias = await _apiClient.ResolveMediaAsync(
                dramaId,
                videoId,
                cancellationToken
            );

            if (freshMedias is null || freshMedias.Count == 0)
            {
                return [];
            }

            // 3. 校验规格：只有完整多清晰度规格（通常 5 种画质或老剧 4 种）才写入磁盘持久化
            if (ShouldCacheMedias(freshMedias))
            {
                dramaCache.Episodes[videoId] = [.. freshMedias];
                dramaCache.LastUpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await SaveDramaCacheToFileAsync(dramaCache);
                _logger.ZLogInformation($"已将完整分集媒体缓存写入磁盘: DramaId={dramaId}, VideoId={videoId}, 规格数={freshMedias.Count}");
            }
            else
            {
                _logger.ZLogWarning(
                    $"获取到的媒体列表仅有 {freshMedias.Count} 条地址，不符合完整多画质缓存条件，仅供本次播放使用，不写入持久化: DramaId={dramaId}, VideoId={videoId}"
                );
            }

            return freshMedias;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task InvalidateCacheAsync(string dramaId, string? videoId = null)
    {
        if (string.IsNullOrWhiteSpace(dramaId))
        {
            return;
        }

        SemaphoreSlim gate = GetLock(dramaId);
        await gate.WaitAsync();
        try
        {
            if (string.IsNullOrWhiteSpace(videoId))
            {
                // 清空整部剧的缓存
                _memoryCache.TryRemove(dramaId, out _);
                string filePath = GetDramaFilePath(dramaId);
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        _logger.ZLogInformation($"已删除整剧媒体缓存文件: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.ZLogWarning(ex, $"删除短剧媒体缓存文件失败: {filePath}");
                    }
                }
            }
            else
            {
                // 仅移除指定分集
                DramaPlaybackMediaCache dramaCache = await LoadDramaCacheAsync(dramaId);
                if (dramaCache.Episodes.Remove(videoId))
                {
                    await SaveDramaCacheToFileAsync(dramaCache);
                    _logger.ZLogInformation($"已从缓存中移除分集媒体: DramaId={dramaId}, VideoId={videoId}");
                }
            }
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasValidCacheAsync(string dramaId, string videoId)
    {
        if (string.IsNullOrWhiteSpace(dramaId) || string.IsNullOrWhiteSpace(videoId))
        {
            return false;
        }

        SemaphoreSlim gate = GetLock(dramaId);
        await gate.WaitAsync();
        try
        {
            DramaPlaybackMediaCache dramaCache = await LoadDramaCacheAsync(dramaId);
            if (dramaCache.Episodes.TryGetValue(videoId, out List<PlaybackMedia>? cachedList))
            {
                return IsMediaListValidAndNotExpired(cachedList);
            }
            return false;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// 判断返回的媒体源列表是否符合持久化存储标准.
    /// 标准：如果只获取到一个地址则不存储；正常情况下每集包含 1080p, 720p, 540p, 480p, 360p（老剧可能无 1080p，但也应有至少 4 档清晰度）才存储.
    /// </summary>
    private static bool ShouldCacheMedias(IReadOnlyList<PlaybackMedia>? list)
    {
        if (list is null || list.Count <= 1)
        {
            return false;
        }

        HashSet<MediaDefinition> definitions = [.. list.Select(m => m.Definition)];

        bool hasBaseline4 =
            definitions.Contains(MediaDefinition.P360)
            && definitions.Contains(MediaDefinition.P480)
            && definitions.Contains(MediaDefinition.P540)
            && definitions.Contains(MediaDefinition.P720);

        return hasBaseline4 || definitions.Count >= 4;
    }

    private static bool IsMediaListValidAndNotExpired(IReadOnlyList<PlaybackMedia>? list)
    {
        if (list is null || list.Count == 0)
        {
            return false;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (PlaybackMedia m in list)
        {
            if (string.IsNullOrWhiteSpace(m.Url))
            {
                return false;
            }

            // 具有过期时间戳（Unix 秒）时，留 60 秒安全裕量提前刷新
            if (m.ExpiresAtUnixSeconds > 0 && now >= (m.ExpiresAtUnixSeconds - 60))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<DramaPlaybackMediaCache> LoadDramaCacheAsync(string dramaId)
    {
        if (_memoryCache.TryGetValue(dramaId, out DramaPlaybackMediaCache? cached))
        {
            return cached;
        }

        string filePath = GetDramaFilePath(dramaId);
        if (File.Exists(filePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                DramaPlaybackMediaCache? deserialized =
                    JsonAotSerializer.Deserialize<DramaPlaybackMediaCache>(json);
                if (deserialized is not null)
                {
                    _memoryCache[dramaId] = deserialized;
                    return deserialized;
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"读取短剧媒体缓存文件失败，将重新建立: {filePath}");
            }
        }

        DramaPlaybackMediaCache newCache = new()
        {
            DramaId = dramaId,
            LastUpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
        _memoryCache[dramaId] = newCache;
        return newCache;
    }

    private async Task SaveDramaCacheToFileAsync(DramaPlaybackMediaCache cache)
    {
        string filePath = GetDramaFilePath(cache.DramaId);
        try
        {
            string json = JsonAotSerializer.Serialize(cache);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"保存短剧媒体缓存文件失败: {filePath}");
        }
    }
}
