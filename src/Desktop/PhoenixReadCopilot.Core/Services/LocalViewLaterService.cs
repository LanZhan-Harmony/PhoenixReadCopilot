using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Helpers;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 本地稍后再看服务实现.
/// </summary>
public sealed class LocalViewLaterService : ILocalViewLaterService
{
    private readonly ILogger<LocalViewLaterService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<Drama>? _cache;
    private string? _cachedFilePath;

    /// <inheritdoc/>
    public event EventHandler? ViewLaterChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalViewLaterService"/> class.
    /// </summary>
    public LocalViewLaterService(
        ILogger<LocalViewLaterService> logger,
        IPathService pathService,
        IAuthenticationService authService
    )
    {
        _logger = logger;
        _authService = authService;
        _folderPath = Path.Combine(pathService.DataFolderPath, "ViewLater");
        EnsureDirectory();
        MigrateLegacyFile(pathService);

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

    private void MigrateLegacyFile(IPathService pathService)
    {
        try
        {
            string oldPath = Path.Combine(pathService.DataFolderPath, "view_later.json");
            string anonymousPath = Path.Combine(_folderPath, "anonymous.json");
            if (File.Exists(oldPath) && !File.Exists(anonymousPath))
            {
                File.Copy(oldPath, anonymousPath, overwrite: false);
                _logger.ZLogInformation($"已将旧版本稍后再看数据迁移至: {anonymousPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"迁移旧版本稍后再看数据失败.");
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

        ViewLaterChanged?.Invoke(this, EventArgs.Empty);
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
    public async Task AddAsync(Drama drama)
    {
        if (drama is null || string.IsNullOrWhiteSpace(drama.Id))
        {
            return;
        }

        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            _cache!.RemoveAll(d => d.Id == drama.Id);
            _cache.Insert(0, drama);
            await SaveToFileAsync();
        }
        finally
        {
            _lock.Release();
        }

        ViewLaterChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string dramaId)
    {
        if (string.IsNullOrWhiteSpace(dramaId))
        {
            return;
        }

        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            int removed = _cache!.RemoveAll(d => d.Id == dramaId);
            if (removed > 0)
            {
                await SaveToFileAsync();
            }
        }
        finally
        {
            _lock.Release();
        }

        ViewLaterChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task ClearAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _cache = [];
            await SaveToFileAsync();
        }
        finally
        {
            _lock.Release();
        }

        ViewLaterChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Drama>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return [.. _cache!];
        }
        finally
        {
            _lock.Release();
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
                _cache = JsonAotSerializer.Deserialize<List<Drama>>(json) ?? [];
                return;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"读取稍后再看缓存文件失败，将重新初始化: {currentFilePath}");
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
            _logger.ZLogError(ex, $"保存稍后再看文件失败: {targetFilePath}");
        }
    }
}
