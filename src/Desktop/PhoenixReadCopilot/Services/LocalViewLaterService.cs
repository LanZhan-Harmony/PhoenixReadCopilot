using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Helpers;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 本地稍后再看服务实现.
/// </summary>
public sealed class LocalViewLaterService : ILocalViewLaterService
{
    private readonly ILogger<LocalViewLaterService> _logger;
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<Drama>? _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalViewLaterService"/> class.
    /// </summary>
    public LocalViewLaterService(ILogger<LocalViewLaterService> logger)
    {
        _logger = logger;
        _filePath = Path.Combine(PathHelper.AuthFolderPath, "view_later.json");
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
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Drama>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return _cache!.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureLoadedAsync()
    {
        if (_cache is not null)
        {
            return;
        }

        if (File.Exists(_filePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(_filePath);
                _cache = JsonAotSerializer.Deserialize<List<Drama>>(json) ?? [];
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取稍后再看缓存文件失败，将重新初始化.");
            }
        }

        _cache = [];
    }

    private async Task SaveToFileAsync()
    {
        try
        {
            string json = JsonAotSerializer.Serialize(_cache);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存稍后再看文件失败.");
        }
    }
}
