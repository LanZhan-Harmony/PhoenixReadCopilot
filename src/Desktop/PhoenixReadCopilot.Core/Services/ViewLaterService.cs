using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Helpers;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 稍后再看服务实现（支持短剧，并预留视频、图文、书籍等扩展，支持登录与未登录隔离分文件存储）.
/// </summary>
public sealed class ViewLaterService : IViewLaterService
{
    private readonly ILogger<ViewLaterService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<Drama>? _dramaCache;
    private readonly HashSet<string> _viewLaterIds = [with(StringComparer.OrdinalIgnoreCase)];
    private string? _cachedUserKey;

    /// <inheritdoc/>
    public event EventHandler? ViewLaterChanged;

    public ViewLaterService(
        ILogger<ViewLaterService> logger,
        IPathService pathService,
        IAuthenticationService authService
    )
    {
        _logger = logger;
        _authService = authService;
        _folderPath = pathService.ViewLaterFolderPath;

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
            _logger.ZLogError(ex, $"创建稍后再看数据目录失败: {_folderPath}");
        }
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        _lock.Wait();
        try
        {
            _dramaCache = null;
            _viewLaterIds.Clear();
            _cachedUserKey = null;
        }
        finally
        {
            _lock.Release();
        }

        ViewLaterChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetStorageFilePath(ViewLaterType type)
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
            ViewLaterType.Drama => "drama.json",
            ViewLaterType.Video => "video.json",
            ViewLaterType.Post => "post.json",
            ViewLaterType.Novel => "novel.json",
            _ => "drama.json",
        };
        return Path.Combine(targetDir, fileName);
    }

    /// <inheritdoc/>
    public bool IsInViewLater(string id, ViewLaterType type = ViewLaterType.Drama)
    {
        if (string.IsNullOrEmpty(id) || type != ViewLaterType.Drama)
        {
            return false;
        }

        if (_dramaCache is not null)
        {
            return _viewLaterIds.Contains(id);
        }

        string filePath = GetStorageFilePath(ViewLaterType.Drama);
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<Drama>? items = JsonAotSerializer.Deserialize<List<Drama>>(json);
            if (items is not null)
            {
                foreach (Drama item in items)
                {
                    if (!string.IsNullOrEmpty(item.Id))
                    {
                        _viewLaterIds.Add(item.Id);
                    }
                }
            }
            return _viewLaterIds.Contains(id);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsInViewLaterAsync(
        string id,
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(id) || type != ViewLaterType.Drama)
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync(ViewLaterType.Drama);
            return _viewLaterIds.Contains(id);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureLoadedLockedAsync(ViewLaterType type = ViewLaterType.Drama)
    {
        string userFolder =
            _authService.IsAuthenticated && !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
                ? _authService.CurrentUser.Id
                : "anonymous";

        if (type == ViewLaterType.Drama)
        {
            if (_dramaCache is not null && _cachedUserKey == userFolder)
            {
                return;
            }

            _viewLaterIds.Clear();

            string filePath = GetStorageFilePath(ViewLaterType.Drama);
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
                            _viewLaterIds.Add(item.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ZLogWarning(ex, $"加载稍后再看文件失败: {filePath}");
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

    private async Task SaveCacheLockedAsync(ViewLaterType type = ViewLaterType.Drama)
    {
        try
        {
            if (type == ViewLaterType.Drama)
            {
                string filePath = GetStorageFilePath(ViewLaterType.Drama);
                string json = JsonAotSerializer.Serialize(_dramaCache ?? []);
                await File.WriteAllTextAsync(filePath, json);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"持久化稍后再看数据失败: 类型={type}");
        }
    }

    /// <inheritdoc/>
    public async Task AddAsync(
        Drama drama,
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (drama is null || string.IsNullOrEmpty(drama.Id))
        {
            return;
        }

        if (type != ViewLaterType.Drama)
        {
            _logger.ZLogInformation($"图文或书籍稍后再看暂无业务数据模型，仅记录操作: 类型={type}");
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync(ViewLaterType.Drama);

            int index = _dramaCache!.FindIndex(p => p.Id == drama.Id);
            if (index >= 0)
            {
                _dramaCache.RemoveAt(index);
            }

            _dramaCache.Insert(0, drama);
            _viewLaterIds.Add(drama.Id);

            await SaveCacheLockedAsync(ViewLaterType.Drama);
        }
        finally
        {
            _lock.Release();
        }

        ViewLaterChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(
        string id,
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(id) || type != ViewLaterType.Drama)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync(ViewLaterType.Drama);

            int count = _dramaCache!.RemoveAll(p => p.Id == id);
            if (count > 0)
            {
                _viewLaterIds.Remove(id);
                await SaveCacheLockedAsync(ViewLaterType.Drama);
            }
        }
        finally
        {
            _lock.Release();
        }

        ViewLaterChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (type != ViewLaterType.Drama)
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync(ViewLaterType.Drama);
            _dramaCache!.Clear();
            _viewLaterIds.Clear();
            await SaveCacheLockedAsync(ViewLaterType.Drama);
        }
        finally
        {
            _lock.Release();
        }

        ViewLaterChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Drama>> GetAllAsync(
        ViewLaterType type = ViewLaterType.Drama,
        CancellationToken cancellationToken = default
    )
    {
        if (type != ViewLaterType.Drama)
        {
            return [];
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync(ViewLaterType.Drama);
            return [.. _dramaCache!];
        }
        finally
        {
            _lock.Release();
        }
    }
}
