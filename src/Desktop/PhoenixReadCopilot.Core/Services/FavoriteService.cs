using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Helpers;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 收藏/追剧服务实现（面向整部短剧，支持登录与未登录隔离存储，预留书籍与图文扩展）.
/// </summary>
public sealed class FavoriteService : IFavoriteService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FavoriteService> _logger;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<Drama>? _dramaCache;
    private readonly HashSet<string> _favoritedDramaIds = [with(StringComparer.OrdinalIgnoreCase)];
    private string? _cachedUserKey;

    /// <inheritdoc/>
    public event EventHandler? FavoriteChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="FavoriteService"/> class.
    /// </summary>
    public FavoriteService(
        PhoenixReadApiClient apiClient,
        IAuthenticationService authService,
        IPathService pathService,
        ILogger<FavoriteService> logger
    )
    {
        _apiClient = apiClient;
        _authService = authService;
        _logger = logger;
        _folderPath = pathService.FavoriteFolderPath;

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
            _logger.ZLogError(ex, $"创建追剧收藏数据目录失败: {_folderPath}");
        }
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        _lock.Wait();
        try
        {
            _dramaCache = null;
            _favoritedDramaIds.Clear();
            _cachedUserKey = null;
        }
        finally
        {
            _lock.Release();
        }

        FavoriteChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetStorageFilePath(FavoriteType type)
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
            FavoriteType.Drama => "drama.json",
            FavoriteType.Video => "video.json",
            FavoriteType.Post => "post.json",
            FavoriteType.Novel => "novel.json",
            _ => "drama.json",
        };
        return Path.Combine(targetDir, fileName);
    }

    /// <inheritdoc/>
    public bool IsFavorited(string dramaId, FavoriteType type = FavoriteType.Drama)
    {
        if (string.IsNullOrEmpty(dramaId) || type != FavoriteType.Drama)
        {
            return false;
        }

        if (_dramaCache != null)
        {
            return _favoritedDramaIds.Contains(dramaId);
        }

        string filePath = GetStorageFilePath(FavoriteType.Drama);
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<Drama>? items = JsonAotSerializer.Deserialize<List<Drama>>(json);
            if (items != null)
            {
                foreach (Drama item in items)
                {
                    if (!string.IsNullOrEmpty(item.Id))
                    {
                        _favoritedDramaIds.Add(item.Id);
                    }
                }
            }
            return _favoritedDramaIds.Contains(dramaId);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsFavoritedAsync(
        string id,
        FavoriteType type = FavoriteType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(id) || type != FavoriteType.Drama)
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();
            return _favoritedDramaIds.Contains(id);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureLoadedLockedAsync()
    {
        string userFolder =
            _authService.IsAuthenticated && !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
                ? _authService.CurrentUser.Id
                : "anonymous";

        if (_dramaCache != null && _cachedUserKey == userFolder)
        {
            return;
        }

        _favoritedDramaIds.Clear();
        string filePath = GetStorageFilePath(FavoriteType.Drama);
        if (File.Exists(filePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath);
                List<Drama>? items = JsonAotSerializer.Deserialize<List<Drama>>(json);
                _dramaCache = items ?? [];
                foreach (Drama item in _dramaCache)
                {
                    if (!string.IsNullOrEmpty(item.Id))
                    {
                        _favoritedDramaIds.Add(item.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"加载本地追剧文件失败: {filePath}");
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
            string filePath = GetStorageFilePath(FavoriteType.Drama);
            string json = JsonAotSerializer.Serialize(_dramaCache ?? []);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"持久化追剧数据失败");
        }
    }

    /// <inheritdoc/>
    public async Task<DramaPage> GetFavoritesAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    )
    {
        if (_authService.IsAuthenticated)
        {
            try
            {
                DramaPage onlinePage = await _apiClient.GetFavoritesAsync(
                    count: count,
                    offset: offset,
                    cancellationToken: cancellationToken
                );

                await _lock.WaitAsync(cancellationToken);
                try
                {
                    await EnsureLoadedLockedAsync();
                    if (onlinePage.Items is { Count: > 0 } items)
                    {
                        foreach (Drama onlineDrama in items)
                        {
                            if (string.IsNullOrEmpty(onlineDrama.Id))
                            {
                                continue;
                            }

                            _favoritedDramaIds.Add(onlineDrama.Id);
                            int idx = _dramaCache!.FindIndex(d => d.Id == onlineDrama.Id);
                            if (idx >= 0)
                            {
                                Drama local = _dramaCache[idx];
                                _dramaCache[idx] = new Drama
                                {
                                    Id = onlineDrama.Id,
                                    Title = !string.IsNullOrEmpty(onlineDrama.Title)
                                        ? onlineDrama.Title
                                        : local.Title,
                                    CoverUrl = !string.IsNullOrEmpty(onlineDrama.CoverUrl)
                                        ? onlineDrama.CoverUrl
                                        : local.CoverUrl,
                                    Description = !string.IsNullOrEmpty(onlineDrama.Description)
                                        ? onlineDrama.Description
                                        : local.Description,
                                    EpisodeCount =
                                        onlineDrama.EpisodeCount > 0
                                            ? onlineDrama.EpisodeCount
                                            : local.EpisodeCount,
                                    Score = !string.IsNullOrEmpty(onlineDrama.Score)
                                        ? onlineDrama.Score
                                        : local.Score,
                                    Category = !string.IsNullOrEmpty(onlineDrama.Category)
                                        ? onlineDrama.Category
                                        : local.Category,
                                    Tags = onlineDrama.Tags is { Count: > 0 }
                                        ? onlineDrama.Tags
                                        : local.Tags,
                                };
                            }
                            else
                            {
                                _dramaCache.Add(onlineDrama);
                            }
                        }

                        await SaveCacheLockedAsync();
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
                _logger.ZLogWarning(ex, $"在线获取追剧列表失败，降级使用本地存储.");
            }
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();
            int safeCount = Math.Max(1, count);
            int safeOffset = Math.Max(0, offset);
            List<Drama> slice = [.. _dramaCache!.Skip(safeOffset).Take(safeCount)];
            return new DramaPage
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
    public async Task<bool> SetFavoriteAsync(
        string dramaId,
        bool isFavorite,
        Drama? drama = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(dramaId))
        {
            return false;
        }

        if (_authService.IsAuthenticated)
        {
            try
            {
                PhoenixOperationResult result = await _apiClient.SetFavoriteAsync(
                    dramaId: dramaId,
                    videoId: string.Empty,
                    active: isFavorite,
                    cancellationToken: cancellationToken
                );

                if (!result.Succeeded)
                {
                    _logger.ZLogWarning($"调用在线设置追剧接口失败: {result.Message}");
                    return false;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.ZLogWarning(ex, $"调用在线追剧接口异常，同步更新本地缓存: {dramaId}");
            }
        }
        else
        {
            // 未登录模式：尝试在线请求（若支持），但不阻塞本地保存
            try
            {
                await _apiClient.SetFavoriteAsync(
                    dramaId: dramaId,
                    videoId: string.Empty,
                    active: isFavorite,
                    cancellationToken: cancellationToken
                );
            }
            catch
            {
                // 匿名状态服务端可能报错，忽略网络错误直接存本地
            }
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();

            if (isFavorite)
            {
                _favoritedDramaIds.Add(dramaId);
                int idx = _dramaCache!.FindIndex(d => d.Id == dramaId);
                if (idx >= 0)
                {
                    if (drama != null)
                    {
                        _dramaCache[idx] = drama;
                    }
                }
                else
                {
                    Drama itemToAdd = drama ?? new Drama { Id = dramaId };
                    _dramaCache.Insert(0, itemToAdd);
                }
            }
            else
            {
                _favoritedDramaIds.Remove(dramaId);
                _dramaCache!.RemoveAll(d => d.Id == dramaId);
            }

            await SaveCacheLockedAsync();
        }
        finally
        {
            _lock.Release();
        }

        FavoriteChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> SetFavoriteAsync(
        string id,
        bool isFavorite,
        FavoriteType type,
        Drama? drama = null,
        CancellationToken cancellationToken = default
    )
    {
        if (type == FavoriteType.Drama)
        {
            return await SetFavoriteAsync(id, isFavorite, drama, cancellationToken);
        }

        // 预留图文与小说扩展
        _logger.ZLogInformation($"图文或书籍收藏暂无服务端接口支持，操作类型={type}, Id={id}");
        return false;
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_authService.IsAuthenticated)
        {
            try
            {
                await GetFavoritesAsync(count: 50, offset: 0, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.ZLogWarning(ex, $"刷新在线追剧数据失败.");
            }
        }
        else
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                _dramaCache = null;
                _favoritedDramaIds.Clear();
                await EnsureLoadedLockedAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        FavoriteChanged?.Invoke(this, EventArgs.Empty);
    }
}
