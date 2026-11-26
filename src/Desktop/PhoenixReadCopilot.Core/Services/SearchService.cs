using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Helpers;
using PhoenixReadCopilot.Core.Models.Search;
using ZLogger;
using ApiHotSearchItem = PhoenixReadApi.HotSearchItem;
using CoreHotSearchItem = PhoenixReadCopilot.Core.Models.Search.HotSearchItem;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 搜索服务实现，提供剧集/用户搜索、热搜榜单与按用户隔离的本地搜索历史记录管理.
/// </summary>
public sealed class SearchService : ISearchService
{
    private const int MaxHistoryCount = 50;
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<SearchService> _logger;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<string>? _historyCache;
    private string? _cachedUserKey;

    /// <inheritdoc/>
    public event EventHandler? SearchHistoryChanged;

    public SearchService(
        PhoenixReadApiClient apiClient,
        IAuthenticationService authService,
        IPathService pathService,
        ILogger<SearchService> logger
    )
    {
        _apiClient = apiClient;
        _authService = authService;
        _logger = logger;
        _folderPath = pathService.SearchFolderPath;

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
            _logger.ZLogError(ex, $"创建搜索数据目录失败: {_folderPath}");
        }
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        _lock.Wait();
        try
        {
            _historyCache = null;
            _cachedUserKey = null;
        }
        finally
        {
            _lock.Release();
        }

        SearchHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetStorageFilePath()
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

        return Path.Combine(targetDir, "history.json");
    }

    private string GetCurrentUserKey() =>
        _authService.IsAuthenticated && !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
            ? _authService.CurrentUser.Id
            : "anonymous";

    /// <inheritdoc/>
    public Task<SearchResult> SearchAsync(
        string keyword,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);
        return _apiClient.SearchAsync(keyword, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<SearchResult> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        return _apiClient.SearchAsync(query, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SearchSuggestion>> GetSuggestionsAsync(
        string keyword,
        int maxResults = 8,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Task.FromResult<IReadOnlyList<SearchSuggestion>>([]);
        }

        return _apiClient.GetSuggestionsAsync(keyword, maxResults, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CoreHotSearchItem>> GetHotSearchesAsync(
        CancellationToken cancellationToken = default
    )
    {
        HotSearch hotSearch = await _apiClient.GetHotSearchAsync(cancellationToken);
        int index = 1;

        // 优先加载短剧热搜榜，若无则使用第一张榜单或猜你想搜
        HotSearchBoard? board = hotSearch.Boards.FirstOrDefault(b => b.Title.Contains("短剧"))
            ?? hotSearch.Boards.FirstOrDefault();

        IEnumerable<ApiHotSearchItem> sourceItems = board?.Items ?? hotSearch.Guesses;
        List<CoreHotSearchItem> result = [];

        foreach (ApiHotSearchItem item in sourceItems)
        {
            SearchItemIcon? icon = null;
            if (
                !string.IsNullOrEmpty(item.CoverUrl)
                && Uri.TryCreate(item.CoverUrl, UriKind.Absolute, out Uri? coverUri)
            )
            {
                icon = new SearchItemIcon { Uri = coverUri };
            }

            string heat = !string.IsNullOrEmpty(item.CardTips) ? item.CardTips : item.Heat;
            result.Add(
                new CoreHotSearchItem
                {
                    Index = index++,
                    Text = item.Title,
                    Keyword = item.Title,
                    Tag = item.Tag,
                    Heat = heat,
                    Icon = icon,
                }
            );
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<HotSearch> GetRawHotSearchAsync(CancellationToken cancellationToken = default) =>
        _apiClient.GetHotSearchAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetSearchHistoryAsync(
        CancellationToken cancellationToken = default
    )
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            string userKey = GetCurrentUserKey();
            if (_historyCache is not null && _cachedUserKey == userKey)
            {
                return [.. _historyCache];
            }

            string filePath = GetStorageFilePath();
            List<string> list = await ReadFileLockedAsync(filePath, cancellationToken);
            _historyCache = list;
            _cachedUserKey = userKey;
            return [.. list];
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task AddSearchHistoryAsync(
        string keyword,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        string trimmed = keyword.Trim();
        await _lock.WaitAsync(cancellationToken);
        try
        {
            string userKey = GetCurrentUserKey();
            if (_historyCache is null || _cachedUserKey != userKey)
            {
                string filePath = GetStorageFilePath();
                _historyCache = await ReadFileLockedAsync(filePath, cancellationToken);
                _cachedUserKey = userKey;
            }

            // 移除非区分大小写的相同词项，并将最新词项置于首位
            _historyCache.RemoveAll(k =>
                string.Equals(k, trimmed, StringComparison.OrdinalIgnoreCase)
            );
            _historyCache.Insert(0, trimmed);

            if (_historyCache.Count > MaxHistoryCount)
            {
                _historyCache.RemoveRange(MaxHistoryCount, _historyCache.Count - MaxHistoryCount);
            }

            string targetPath = GetStorageFilePath();
            await WriteFileLockedAsync(targetPath, _historyCache, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"保存搜索历史记录失败: {trimmed}");
        }
        finally
        {
            _lock.Release();
        }

        SearchHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task RemoveSearchHistoryAsync(
        string keyword,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return;
        }

        string trimmed = keyword.Trim();
        await _lock.WaitAsync(cancellationToken);
        try
        {
            string userKey = GetCurrentUserKey();
            if (_historyCache is null || _cachedUserKey != userKey)
            {
                string filePath = GetStorageFilePath();
                _historyCache = await ReadFileLockedAsync(filePath, cancellationToken);
                _cachedUserKey = userKey;
            }

            int removed = _historyCache.RemoveAll(k =>
                string.Equals(k, trimmed, StringComparison.OrdinalIgnoreCase)
            );
            if (removed > 0)
            {
                string targetPath = GetStorageFilePath();
                await WriteFileLockedAsync(targetPath, _historyCache, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"移除搜索历史记录失败: {trimmed}");
        }
        finally
        {
            _lock.Release();
        }

        SearchHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task ClearSearchHistoryAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _historyCache = [];
            _cachedUserKey = GetCurrentUserKey();

            string targetPath = GetStorageFilePath();
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"清空搜索历史记录失败.");
        }
        finally
        {
            _lock.Release();
        }

        SearchHistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task<List<string>> ReadFileLockedAsync(
        string filePath,
        CancellationToken cancellationToken
    )
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            return JsonAotSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"读取搜索历史记录文件失败: {filePath}");
            return [];
        }
    }

    private static async Task WriteFileLockedAsync(
        string filePath,
        List<string> items,
        CancellationToken cancellationToken
    )
    {
        string dir = Path.GetDirectoryName(filePath)!;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonAotSerializer.Serialize(items);
        string tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, filePath, overwrite: true);
    }
}
