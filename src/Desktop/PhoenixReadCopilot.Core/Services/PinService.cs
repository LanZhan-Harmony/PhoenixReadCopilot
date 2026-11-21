using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Helpers;
using PhoenixReadCopilot.Core.Models.Pin;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 固定条目管理服务实现（支持短剧、用户，并预留视频、图文、书籍等扩展，支持登录与未登录隔离分文件存储）.
/// </summary>
public sealed class PinService : IPinService
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<PinService> _logger;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly Dictionary<PinType, List<PinItem>> _caches = [];
    private readonly HashSet<string> _pinnedIds = [with(StringComparer.OrdinalIgnoreCase)];
    private string? _cachedUserKey;

    /// <inheritdoc/>
    public event EventHandler? PinChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PinService"/> class.
    /// </summary>
    public PinService(
        IPathService pathService,
        IAuthenticationService authService,
        ILogger<PinService> logger
    )
    {
        _authService = authService;
        _logger = logger;
        _folderPath = pathService.PinFolderPath;

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
            _logger.ZLogError(ex, $"创建固定条目数据目录失败: {_folderPath}");
        }
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        _lock.Wait();
        try
        {
            _caches.Clear();
            _pinnedIds.Clear();
            _cachedUserKey = null;
        }
        finally
        {
            _lock.Release();
        }

        PinChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetStorageFilePath(PinType type)
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
            PinType.User => "user.json",
            PinType.Drama => "drama.json",
            PinType.Video => "video.json",
            PinType.Post => "post.json",
            PinType.Novel => "novel.json",
            _ => "drama.json",
        };
        return Path.Combine(targetDir, fileName);
    }

    /// <inheritdoc/>
    public bool IsPinned(string id, PinType? type = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        if (_caches.Count > 0)
        {
            if (!type.HasValue)
            {
                return _pinnedIds.Contains(id);
            }

            return _caches.TryGetValue(type.Value, out List<PinItem>? list)
                && list.Any(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        // 缓存未加载时尝试快速读取对应文件
        PinType[] typesToCheck = type.HasValue ? [type.Value] : Enum.GetValues<PinType>();
        foreach (PinType t in typesToCheck)
        {
            string filePath = GetStorageFilePath(t);
            if (!File.Exists(filePath))
            {
                continue;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                List<PinItem>? items = JsonAotSerializer.Deserialize<List<PinItem>>(json);
                if (items != null)
                {
                    foreach (PinItem item in items)
                    {
                        if (!string.IsNullOrEmpty(item.Id))
                        {
                            _pinnedIds.Add(item.Id);
                        }
                    }
                }

                if (
                    items?.Any(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
                    == true
                )
                {
                    return true;
                }
            }
            catch
            {
                // 忽略单个文件解析异常
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> IsPinnedAsync(
        string id,
        PinType? type = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();
            if (!type.HasValue)
            {
                return _pinnedIds.Contains(id);
            }

            return _caches.TryGetValue(type.Value, out List<PinItem>? list)
                && list.Any(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
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

        if (_caches.Count > 0 && _cachedUserKey == userFolder)
        {
            return;
        }

        _caches.Clear();
        _pinnedIds.Clear();

        foreach (PinType type in Enum.GetValues<PinType>())
        {
            string filePath = GetStorageFilePath(type);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    List<PinItem>? items = JsonAotSerializer.Deserialize<List<PinItem>>(json);
                    _caches[type] = items ?? [];
                    foreach (PinItem item in _caches[type])
                    {
                        if (!string.IsNullOrEmpty(item.Id))
                        {
                            _pinnedIds.Add(item.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ZLogWarning(ex, $"加载固定条目文件失败: {filePath}");
                    _caches[type] = [];
                }
            }
            else
            {
                _caches[type] = [];
            }
        }

        _cachedUserKey = userFolder;
    }

    private async Task SaveCacheLockedAsync(PinType type)
    {
        try
        {
            string filePath = GetStorageFilePath(type);
            List<PinItem> list = _caches.TryGetValue(type, out List<PinItem>? items) ? items : [];
            string json = JsonAotSerializer.Serialize(list);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"持久化固定条目数据失败: 类型={type}");
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PinItem>> GetPinnedItemsAsync(
        PinType? type = null,
        CancellationToken cancellationToken = default
    )
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();
            if (type.HasValue)
            {
                return _caches.TryGetValue(type.Value, out List<PinItem>? list) ? [.. list] : [];
            }

            // 返回所有类型的固定项
            List<PinItem> all = [];
            foreach (List<PinItem> list in _caches.Values)
            {
                all.AddRange(list);
            }
            return all;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> PinAsync(PinItem item, CancellationToken cancellationToken = default)
    {
        if (item is null || string.IsNullOrEmpty(item.Id))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();

            if (_pinnedIds.Contains(item.Id))
            {
                return false;
            }

            if (!_caches.TryGetValue(item.Type, out List<PinItem>? list))
            {
                list = [];
                _caches[item.Type] = list;
            }

            list.Insert(0, item);
            _pinnedIds.Add(item.Id);
            await SaveCacheLockedAsync(item.Type);
        }
        finally
        {
            _lock.Release();
        }

        PinChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UnpinAsync(
        string id,
        PinType? type = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        bool removed = false;
        try
        {
            await EnsureLoadedLockedAsync();

            PinType[] typesToSearch = type.HasValue ? [type.Value] : _caches.Keys.ToArray();
            foreach (PinType t in typesToSearch)
            {
                if (_caches.TryGetValue(t, out List<PinItem>? list))
                {
                    int count = list.RemoveAll(p =>
                        string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)
                    );
                    if (count > 0)
                    {
                        removed = true;
                        await SaveCacheLockedAsync(t);
                    }
                }
            }

            if (removed)
            {
                _pinnedIds.Remove(id);
            }
        }
        finally
        {
            _lock.Release();
        }

        if (removed)
        {
            PinChanged?.Invoke(this, EventArgs.Empty);
        }

        return removed;
    }

    /// <inheritdoc/>
    public Task<bool> UnpinAsync(PinItem item, CancellationToken cancellationToken = default)
    {
        if (item is null || string.IsNullOrEmpty(item.Id))
        {
            return Task.FromResult(false);
        }

        return UnpinAsync(item.Id, item.Type, cancellationToken);
    }
}
