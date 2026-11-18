using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Helpers;
using PhoenixReadCopilot.Core.Models.Pin;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 固定条目管理服务实现（支持登录与未登录隔离存储）.
/// </summary>
public sealed class PinService : IPinService
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<PinService> _logger;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<PinItem>? _cache;
    private string? _cachedFilePath;

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
        _folderPath = Path.Combine(pathService.DataFolderPath, "Pin");

        EnsureDirectory();
        MigrateLegacyFiles();

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

    private void MigrateLegacyFiles()
    {
        try
        {
            string oldDefault = Path.Combine(_folderPath, "pin_default.json");
            string anonymousPath = Path.Combine(_folderPath, "anonymous.json");
            if (File.Exists(oldDefault) && !File.Exists(anonymousPath))
            {
                File.Copy(oldDefault, anonymousPath, overwrite: false);
                _logger.ZLogInformation($"已将旧版本匿名固定数据迁移至: {anonymousPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"迁移旧版本固定数据失败.");
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

        PinChanged?.Invoke(this, EventArgs.Empty);
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
    public async Task<IReadOnlyList<PinItem>> GetPinnedItemsAsync(
        CancellationToken cancellationToken = default
    )
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();
            return [.. _cache!];
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public bool IsPinned(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        if (_cache != null)
        {
            return _cache.Any(p => p.Id == id);
        }

        string filePath = GetStorageFilePath();
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<PinItem>? items = JsonAotSerializer.Deserialize<List<PinItem>>(json);
            return items?.Any(p => p.Id == id) ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsPinnedAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();
            return _cache!.Any(p => p.Id == id);
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

            int existingIndex = _cache!.FindIndex(p => p.Id == item.Id);
            if (existingIndex >= 0)
            {
                return false;
            }

            _cache.Add(item);
            await SaveLockedAsync();
        }
        finally
        {
            _lock.Release();
        }

        PinChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UnpinAsync(string id, CancellationToken cancellationToken = default)
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

            int removedCount = _cache!.RemoveAll(p => p.Id == id);
            if (removedCount > 0)
            {
                removed = true;
                await SaveLockedAsync();
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
        return item is not null ? UnpinAsync(item.Id, cancellationToken) : Task.FromResult(false);
    }

    private async Task EnsureLoadedLockedAsync()
    {
        string currentFile = GetStorageFilePath();
        if (_cache != null && _cachedFilePath == currentFile)
        {
            return;
        }

        _cache = [];
        _cachedFilePath = currentFile;

        if (!File.Exists(currentFile))
        {
            return;
        }

        try
        {
            string json = await File.ReadAllTextAsync(currentFile);
            List<PinItem>? loaded = JsonAotSerializer.Deserialize<List<PinItem>>(json);
            if (loaded != null)
            {
                _cache = loaded;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"读取固定数据文件失败: {currentFile}");
        }
    }

    private async Task SaveLockedAsync()
    {
        string currentFile = GetStorageFilePath();
        try
        {
            EnsureDirectory();
            string json = JsonAotSerializer.Serialize(_cache ?? []);
            await File.WriteAllTextAsync(currentFile, json);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"保存固定数据文件失败: {currentFile}");
        }
    }
}
