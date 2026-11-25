using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Helpers;
using PhoenixReadCopilot.Core.Models.Like;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 点赞服务实现，负责短剧分集点赞、预留内容类型点赞状态维护与本地/云端隔离存储.
/// </summary>
public sealed class LikeService : ILikeService
{
    private static readonly IReadOnlySet<string> EmptyVideoIdSet = new HashSet<string>();

    private readonly PhoenixReadApiClient _apiClient;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<LikeService> _logger;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<DramaLikeItem>? _dramaCache;
    private readonly Dictionary<string, HashSet<string>> _likedVideoIdsByDrama =
        new(StringComparer.OrdinalIgnoreCase);
    private string? _cachedUserKey;

    /// <inheritdoc/>
    public event EventHandler? LikeChanged;

    public LikeService(
        PhoenixReadApiClient apiClient,
        IAuthenticationService authService,
        IPathService pathService,
        ILogger<LikeService> logger
    )
    {
        _apiClient = apiClient;
        _authService = authService;
        _logger = logger;
        _folderPath = pathService.LikeFolderPath;

        EnsureDirectory();

        _authService.StateChanged += OnAuthStateChanged;

        // 启动后台预热加载短剧点赞数据
        _ = Task.Run(async () =>
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureLoadedLockedAsync();
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"后台预热加载点赞数据失败");
            }
            finally
            {
                _lock.Release();
            }
        });
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
            _logger.ZLogError(ex, $"创建点赞数据根目录失败: {_folderPath}");
        }
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        _lock.Wait();
        try
        {
            _dramaCache = null;
            _likedVideoIdsByDrama.Clear();
            _cachedUserKey = null;
        }
        finally
        {
            _lock.Release();
        }

        LikeChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetStorageFilePath(LikeType type)
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
            LikeType.Drama => "drama.json",
            LikeType.Video => "video.json",
            LikeType.Post => "post.json",
            LikeType.Novel => "novel.json",
            _ => "drama.json",
        };
        return Path.Combine(targetDir, fileName);
    }

    private async Task EnsureLoadedLockedAsync()
    {
        string userFolder =
            _authService.IsAuthenticated && !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
                ? _authService.CurrentUser.Id
                : "anonymous";

        if (_dramaCache is not null && _cachedUserKey == userFolder)
        {
            return;
        }

        _likedVideoIdsByDrama.Clear();
        string filePath = GetStorageFilePath(LikeType.Drama);
        if (File.Exists(filePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                List<DramaLikeItem>? items = JsonAotSerializer.Deserialize<List<DramaLikeItem>>(json);
                _dramaCache = items ?? [];
                foreach (DramaLikeItem item in _dramaCache)
                {
                    if (!string.IsNullOrEmpty(item.DramaId) && item.LikedVideoIds is { Count: > 0 })
                    {
                        _likedVideoIdsByDrama[item.DramaId] = new HashSet<string>(
                            item.LikedVideoIds,
                            StringComparer.OrdinalIgnoreCase
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"加载本地点赞数据文件失败: {filePath}");
                _dramaCache = [];
            }
        }
        else
        {
            _dramaCache = [];
        }

        _cachedUserKey = userFolder;
    }

    private async Task SaveCacheLockedAsync()
    {
        try
        {
            string filePath = GetStorageFilePath(LikeType.Drama);
            string json = JsonAotSerializer.Serialize(_dramaCache ?? []);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"持久化点赞数据失败");
        }
    }

    /// <inheritdoc/>
    public bool IsLiked(string dramaId, string videoId)
    {
        if (string.IsNullOrEmpty(dramaId) || string.IsNullOrEmpty(videoId))
        {
            return false;
        }

        if (_dramaCache is not null)
        {
            return _likedVideoIdsByDrama.TryGetValue(dramaId, out HashSet<string>? set)
                && set.Contains(videoId);
        }

        string filePath = GetStorageFilePath(LikeType.Drama);
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<DramaLikeItem>? items = JsonAotSerializer.Deserialize<List<DramaLikeItem>>(json);
            if (items is not null)
            {
                _dramaCache = items;
                _likedVideoIdsByDrama.Clear();
                foreach (DramaLikeItem item in items)
                {
                    if (!string.IsNullOrEmpty(item.DramaId) && item.LikedVideoIds is { Count: > 0 })
                    {
                        _likedVideoIdsByDrama[item.DramaId] = new HashSet<string>(
                            item.LikedVideoIds,
                            StringComparer.OrdinalIgnoreCase
                        );
                    }
                }

                return _likedVideoIdsByDrama.TryGetValue(dramaId, out HashSet<string>? set)
                    && set.Contains(videoId);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public bool IsLiked(string id, LikeType type)
    {
        if (string.IsNullOrEmpty(id) || type == LikeType.Drama)
        {
            return false;
        }

        string filePath = GetStorageFilePath(type);
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<string>? items = JsonAotSerializer.Deserialize<List<string>>(json);
            return items?.Contains(id, StringComparer.OrdinalIgnoreCase) ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsLikedAsync(
        string dramaId,
        string videoId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(dramaId) || string.IsNullOrEmpty(videoId))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();
            return _likedVideoIdsByDrama.TryGetValue(dramaId, out HashSet<string>? set)
                && set.Contains(videoId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsLikedAsync(
        string id,
        LikeType type,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(id) || type == LikeType.Drama)
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string filePath = GetStorageFilePath(type);
            if (!File.Exists(filePath))
            {
                return false;
            }

            string json = await File.ReadAllTextAsync(filePath, cancellationToken);
            List<string>? items = JsonAotSerializer.Deserialize<List<string>>(json);
            return items?.Contains(id, StringComparer.OrdinalIgnoreCase) ?? false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public IReadOnlySet<string> GetLikedVideoIds(string dramaId)
    {
        if (string.IsNullOrEmpty(dramaId))
        {
            return EmptyVideoIdSet;
        }

        if (_dramaCache is null)
        {
            _ = IsLiked(dramaId, string.Empty);
        }

        if (_likedVideoIdsByDrama.TryGetValue(dramaId, out HashSet<string>? set))
        {
            return set;
        }

        return EmptyVideoIdSet;
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SetDramaEpisodeLikeAsync(
        string dramaId,
        string videoId,
        bool active = true,
        Drama? drama = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        if (_authService.IsAuthenticated)
        {
            try
            {
                PhoenixOperationResult apiResult = await _apiClient.SetDramaEpisodeLikeAsync(
                    dramaId,
                    videoId,
                    active,
                    cancellationToken
                );

                if (!apiResult.Succeeded)
                {
                    _logger.ZLogWarning($"在线分集点赞接口返回失败: {apiResult.Message}");
                    return apiResult;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"在线分集点赞异常，仍更新本地状态");
            }
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();

            if (!_likedVideoIdsByDrama.TryGetValue(dramaId, out HashSet<string>? set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _likedVideoIdsByDrama[dramaId] = set;
            }

            if (active)
            {
                set.Add(videoId);
            }
            else
            {
                set.Remove(videoId);
            }

            List<DramaLikeItem> cache = _dramaCache ??= [];
            DramaLikeItem? item = cache.FirstOrDefault(d =>
                string.Equals(d.DramaId, dramaId, StringComparison.OrdinalIgnoreCase)
            );

            if (active)
            {
                if (item is null)
                {
                    item = new DramaLikeItem
                    {
                        DramaId = dramaId,
                        Drama = drama ?? new Drama { Id = dramaId },
                        LikedVideoIds = [.. set],
                        UpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    };
                    cache.Insert(0, item);
                }
                else
                {
                    if (drama is not null)
                    {
                        item.Drama = drama;
                    }
                    item.LikedVideoIds = [.. set];
                    item.UpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    cache.Remove(item);
                    cache.Insert(0, item);
                }
            }
            else
            {
                if (item is not null)
                {
                    item.LikedVideoIds = [.. set];
                    item.UpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (set.Count == 0)
                    {
                        cache.Remove(item);
                        _likedVideoIdsByDrama.Remove(dramaId);
                    }
                }
            }

            await SaveCacheLockedAsync();
        }
        finally
        {
            _lock.Release();
        }

        LikeChanged?.Invoke(this, EventArgs.Empty);
        return new PhoenixOperationResult
        {
            Succeeded = true,
            Message = active ? "点赞成功" : "取消点赞成功",
        };
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SetLikeAsync(
        string id,
        bool active,
        LikeType type,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (type == LikeType.Drama)
        {
            return new PhoenixOperationResult
            {
                Succeeded = false,
                Message = "短剧类型请使用 SetDramaEpisodeLikeAsync 进行分集点赞",
            };
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string filePath = GetStorageFilePath(type);
            List<string> items = [];
            if (File.Exists(filePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(filePath, cancellationToken);
                    items = JsonAotSerializer.Deserialize<List<string>>(json) ?? [];
                }
                catch
                {
                    items = [];
                }
            }

            if (active)
            {
                if (!items.Contains(id, StringComparer.OrdinalIgnoreCase))
                {
                    items.Insert(0, id);
                }
            }
            else
            {
                items.RemoveAll(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
            }

            string updatedJson = JsonAotSerializer.Serialize(items);
            await File.WriteAllTextAsync(filePath, updatedJson, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }

        LikeChanged?.Invoke(this, EventArgs.Empty);
        return new PhoenixOperationResult
        {
            Succeeded = true,
            Message = active ? "点赞成功" : "取消点赞成功",
        };
    }

    /// <inheritdoc/>
    public async Task<DramaPage> GetLikesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    )
    {
        if (_authService.IsAuthenticated)
        {
            try
            {
                DramaPage onlinePage = await _apiClient.GetLikesAsync(
                    count,
                    offset,
                    cancellationToken
                );

                if (onlinePage?.Items is { Count: > 0 })
                {
                    await _lock.WaitAsync(cancellationToken);
                    try
                    {
                        await EnsureLoadedLockedAsync();
                        List<DramaLikeItem> cache = _dramaCache ??= [];
                        foreach (Drama onlineDrama in onlinePage.Items)
                        {
                            if (string.IsNullOrEmpty(onlineDrama.Id))
                            {
                                continue;
                            }

                            DramaLikeItem? local = cache.FirstOrDefault(d =>
                                string.Equals(d.DramaId, onlineDrama.Id, StringComparison.OrdinalIgnoreCase)
                            );

                            if (local is null)
                            {
                                cache.Add(
                                    new DramaLikeItem
                                    {
                                        DramaId = onlineDrama.Id,
                                        Drama = onlineDrama,
                                        LikedVideoIds = [],
                                        UpdatedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                    }
                                );
                            }
                            else
                            {
                                local.Drama = onlineDrama;
                            }
                        }

                        await SaveCacheLockedAsync();
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }

                return onlinePage ?? new DramaPage();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.ZLogWarning(ex, $"在线获取点赞列表失败，降级使用本地存储.");
            }
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();
            int safeCount = Math.Max(1, count);
            int safeOffset = Math.Max(0, offset);

            List<DramaLikeItem> cache = _dramaCache ??= [];
            List<Drama> slice = cache
                .Where(d => d.Drama is not null && (d.LikedVideoIds.Count > 0 || (_authService.IsAuthenticated && !string.IsNullOrEmpty(d.DramaId))))
                .Select(d => d.Drama!)
                .Skip(safeOffset)
                .Take(safeCount)
                .ToList();

            return new DramaPage
            {
                Items = slice,
                Total = cache.Count,
                NextOffset = safeOffset + slice.Count,
                HasMore = safeOffset + slice.Count < cache.Count,
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _dramaCache = null;
            _likedVideoIdsByDrama.Clear();
            _cachedUserKey = null;
            await EnsureLoadedLockedAsync();
        }
        finally
        {
            _lock.Release();
        }

        LikeChanged?.Invoke(this, EventArgs.Empty);
    }
}
