using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Helpers;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 剧集预约服务实现（支持登录与未登录隔离存储，登录态双写与自动回退）.
/// </summary>
public sealed class ReservationService : IReservationService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ReservationService> _logger;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<ReservationItem>? _cache;
    private readonly HashSet<string> _reservedDramaIds = [with(StringComparer.OrdinalIgnoreCase)];
    private string? _cachedFilePath;

    /// <inheritdoc/>
    public event EventHandler? ReservationChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReservationService"/> class.
    /// </summary>
    public ReservationService(
        PhoenixReadApiClient apiClient,
        IAuthenticationService authService,
        IPathService pathService,
        ILogger<ReservationService> logger
    )
    {
        _apiClient = apiClient;
        _authService = authService;
        _logger = logger;
        _folderPath = pathService.ReservationFolderPath;

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
            _logger.ZLogError(ex, $"创建预约数据目录失败: {_folderPath}");
        }
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        _lock.Wait();
        try
        {
            _cache = null;
            _reservedDramaIds.Clear();
            _cachedFilePath = null;
        }
        finally
        {
            _lock.Release();
        }

        ReservationChanged?.Invoke(this, EventArgs.Empty);
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

        return Path.Combine(targetDir, "drama.json");
    }

    /// <inheritdoc/>
    public bool IsReserved(string dramaId)
    {
        if (string.IsNullOrEmpty(dramaId))
        {
            return false;
        }

        if (_cache != null)
        {
            return _reservedDramaIds.Contains(dramaId);
        }

        string filePath = GetStorageFilePath();
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            List<ReservationItem>? items = JsonAotSerializer.Deserialize<List<ReservationItem>>(
                json
            );
            if (items != null)
            {
                foreach (ReservationItem item in items)
                {
                    if (!string.IsNullOrEmpty(item.DramaId))
                    {
                        _reservedDramaIds.Add(item.DramaId);
                    }
                }
            }
            return _reservedDramaIds.Contains(dramaId);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsReservedAsync(
        string dramaId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(dramaId))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();
            return _reservedDramaIds.Contains(dramaId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ReservationItem>> GetReservationsAsync(
        int count = 20,
        int offset = 0,
        CancellationToken cancellationToken = default
    )
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureLoadedLockedAsync();

            if (_authService.IsAuthenticated)
            {
                try
                {
                    ReservationPage onlinePage = await _apiClient.GetPendingReservationsAsync(
                        count: count,
                        offset: offset,
                        cancellationToken: cancellationToken
                    );

                    if (onlinePage.Items is { Count: > 0 })
                    {
                        foreach (ReservationItem onlineItem in onlinePage.Items)
                        {
                            int existing = _cache!.FindIndex(p => p.DramaId == onlineItem.DramaId);
                            if (existing >= 0)
                            {
                                _cache[existing] = onlineItem;
                            }
                            else
                            {
                                _cache.Add(onlineItem);
                            }
                            _reservedDramaIds.Add(onlineItem.DramaId);
                        }

                        await SaveLockedAsync();
                        return onlinePage.Items;
                    }
                }
                catch (Exception ex)
                {
                    _logger.ZLogWarning(ex, $"从在线 API 获取预约列表失败，将使用本地缓存.");
                }
            }

            return [.. _cache!.Skip(offset).Take(count)];
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SetReservationAsync(
        string dramaId,
        bool isReserved,
        string? title = null,
        string? coverUrl = null,
        string? videoId = null,
        long releaseAtUnixSeconds = 0,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(dramaId))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        bool succeeded = false;
        try
        {
            await EnsureLoadedLockedAsync();

            if (_authService.IsAuthenticated)
            {
                PhoenixOperationResult result = await _apiClient.SetReservationAsync(
                    dramaId: dramaId,
                    active: isReserved,
                    cancellationToken: cancellationToken
                );

                if (!result.Succeeded)
                {
                    _logger.ZLogWarning($"调用在线设置预约接口失败: {result.Message}");
                    return false;
                }
                succeeded = true;
            }
            else
            {
                // 未登录模式：尝试在线请求（若支持），但不阻塞本地保存
                try
                {
                    await _apiClient.SetReservationAsync(
                        dramaId: dramaId,
                        active: isReserved,
                        cancellationToken: cancellationToken
                    );
                }
                catch
                {
                    // 匿名状态服务端可能报错，忽略网络错误直接存本地
                }
                succeeded = true;
            }

            if (isReserved)
            {
                int index = _cache!.FindIndex(p => p.DramaId == dramaId);
                ReservationItem item = new()
                {
                    Id = dramaId,
                    DramaId = dramaId,
                    VideoId = videoId ?? string.Empty,
                    Title = title ?? string.Empty,
                    CoverUrl = coverUrl ?? string.Empty,
                    ReservedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ReleaseAtUnixSeconds = releaseAtUnixSeconds,
                    IsAvailable = true,
                };

                if (index >= 0)
                {
                    _cache[index] = item;
                }
                else
                {
                    _cache.Add(item);
                }

                _reservedDramaIds.Add(dramaId);
            }
            else
            {
                _cache!.RemoveAll(p => p.DramaId == dramaId);
                _reservedDramaIds.Remove(dramaId);
            }

            await SaveLockedAsync();
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"设置剧集预约状态异常: dramaId={dramaId}, isReserved={isReserved}"
            );
            return false;
        }
        finally
        {
            _lock.Release();
        }

        if (succeeded)
        {
            ReservationChanged?.Invoke(this, EventArgs.Empty);
        }

        return succeeded;
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _ = await GetReservationsAsync(count: 100, offset: 0, cancellationToken: cancellationToken);
    }

    private async Task EnsureLoadedLockedAsync()
    {
        string currentFile = GetStorageFilePath();
        if (_cache != null && _cachedFilePath == currentFile)
        {
            return;
        }

        _cache = [];
        _reservedDramaIds.Clear();
        _cachedFilePath = currentFile;

        if (!File.Exists(currentFile))
        {
            return;
        }

        try
        {
            string json = await File.ReadAllTextAsync(currentFile);
            List<ReservationItem>? loaded = JsonAotSerializer.Deserialize<List<ReservationItem>>(
                json
            );
            if (loaded != null)
            {
                _cache = loaded;
                foreach (ReservationItem item in _cache)
                {
                    if (!string.IsNullOrEmpty(item.DramaId))
                    {
                        _reservedDramaIds.Add(item.DramaId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"读取预约数据文件失败: {currentFile}");
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
            _logger.ZLogError(ex, $"保存预约数据文件失败: {currentFile}");
        }
    }
}
