using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Helpers;
using ZLogger;
using User = PhoenixReadApi.User;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 关注与粉丝服务实现，支持创作者与演职员关注、离线/登录双模态隔离存储与粉丝管理.
/// </summary>
public sealed class FollowService : IFollowService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<FollowService> _logger;
    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private readonly HashSet<string> _followingIds = [with(StringComparer.OrdinalIgnoreCase)];
    private List<UserRelationItem>? _followingsCache;
    private string? _cachedUserKey;

    /// <inheritdoc/>
    public event EventHandler? FollowChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="FollowService"/> class.
    /// </summary>
    public FollowService(
        PhoenixReadApiClient apiClient,
        IAuthenticationService authService,
        IPathService pathService,
        ILogger<FollowService> logger
    )
    {
        _apiClient = apiClient;
        _authService = authService;
        _logger = logger;
        _folderPath = pathService.FollowFolderPath;

        EnsureDirectory();

        _authService.StateChanged += OnAuthStateChanged;

        // 启动后台预热加载关注数据，确保应用启动后关注状态立即可用
        _ = Task.Run(async () =>
        {
            await _lock.WaitAsync();
            try
            {
                await EnsureFollowingsInitializedLockedAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"后台预热加载关注数据失败");
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
            _logger.ZLogError(ex, $"创建关注数据主目录失败: {_folderPath}");
        }
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        _lock.Wait();
        try
        {
            _followingsCache = null;
            _followingIds.Clear();
            _cachedUserKey = null;
        }
        finally
        {
            _lock.Release();
        }

        FollowChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetCurrentUserKey() =>
        _authService.IsAuthenticated && !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
            ? _authService.CurrentUser.Id
            : "anonymous";

    private string GetStorageFilePath(string fileName)
    {
        string userFolder = GetCurrentUserKey();
        string targetDir = Path.Combine(_folderPath, userFolder);
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        return Path.Combine(targetDir, fileName);
    }

    private async Task EnsureFollowingsInitializedLockedAsync(CancellationToken cancellationToken)
    {
        string userKey = GetCurrentUserKey();
        if (_followingsCache is not null && _cachedUserKey == userKey)
        {
            return;
        }

        string filePath = GetStorageFilePath("followings.json");
        List<UserRelationItem> items = await ReadFileLockedAsync(filePath, cancellationToken);

        _followingsCache = items;
        _followingIds.Clear();
        foreach (UserRelationItem item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.User?.Id))
            {
                _followingIds.Add(item.User.Id);
            }
        }

        _cachedUserKey = userKey;
    }

    /// <inheritdoc/>
    public bool IsFollowing(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        // 若内存缓存尚未初始化（例如冷启动时立刻同步读取），执行一次同步安全兜底加载
        if (_followingsCache is null)
        {
            _lock.Wait();
            try
            {
                if (_followingsCache is null)
                {
                    string filePath = GetStorageFilePath("followings.json");
                    if (File.Exists(filePath))
                    {
                        string json = File.ReadAllText(filePath);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            List<UserRelationItem>? items = JsonAotSerializer.Deserialize<List<UserRelationItem>>(json);
                            if (items is not null)
                            {
                                _followingsCache = items;
                                _followingIds.Clear();
                                foreach (UserRelationItem item in items)
                                {
                                    if (!string.IsNullOrWhiteSpace(item.User?.Id))
                                    {
                                        _followingIds.Add(item.User.Id);
                                    }
                                }
                                _cachedUserKey = GetCurrentUserKey();
                            }
                        }
                    }

                    if (_followingsCache is null)
                    {
                        _followingsCache = [];
                        _cachedUserKey = GetCurrentUserKey();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"同步兜底加载关注数据失败");
            }
            finally
            {
                _lock.Release();
            }
        }

        return _followingIds.Contains(userId);
    }

    /// <inheritdoc/>
    public async Task<int> GetLocalFollowingsCountAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureFollowingsInitializedLockedAsync(cancellationToken);
            return _followingIds.Count;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsFollowingAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureFollowingsInitializedLockedAsync(cancellationToken);
            return _followingIds.Contains(userId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<PhoenixOperationResult> SetFollowAsync(
        string userId,
        bool active = true,
        User? user = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        PhoenixOperationResult result = new() { Succeeded = true };

        // 1. 若当前已登录，尝试向服务端发起同步写请求
        if (_authService.IsAuthenticated)
        {
            try
            {
                result = await _apiClient.SetFollowAsync(userId, active, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(
                    ex,
                    $"调用服务端关注接口失败，降级本地存储: 用户={userId}, 状态={active}"
                );
            }
        }

        // 2. 双写本地缓存并更新内存状态
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureFollowingsInitializedLockedAsync(cancellationToken);

            if (active)
            {
                _followingIds.Add(userId);
                if (
                    _followingsCache is not null
                    && !_followingsCache.Any(u => u.User?.Id == userId)
                )
                {
                    User userModel = user ?? new User { Id = userId, Name = $"用户 {userId}" };
                    _followingsCache.Insert(
                        0,
                        new UserRelationItem
                        {
                            User = userModel,
                            CanFollow = true,
                            IsFollowing = true,
                        }
                    );
                }
            }
            else
            {
                _followingIds.Remove(userId);
                _followingsCache?.RemoveAll(u => u.User?.Id == userId);
            }

            if (_followingsCache is not null)
            {
                string filePath = GetStorageFilePath("followings.json");
                await WriteFileLockedAsync(filePath, _followingsCache, cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }

        FollowChanged?.Invoke(this, EventArgs.Empty);
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> ToggleFollowAsync(
        string userId,
        User? user = null,
        CancellationToken cancellationToken = default
    )
    {
        bool currentState = await IsFollowingAsync(userId, cancellationToken);
        bool nextState = !currentState;
        await SetFollowAsync(userId, nextState, user, cancellationToken);
        return nextState;
    }

    /// <inheritdoc/>
    public async Task<UserRelationPage> GetFollowingsAsync(
        string? userId = null,
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    )
    {
        bool isCurrentUser =
            string.IsNullOrWhiteSpace(userId)
            || (_authService.IsAuthenticated && _authService.CurrentUser?.Id == userId);

        // 如果查询的是其他用户，直接请求服务端接口
        if (!isCurrentUser && !string.IsNullOrWhiteSpace(userId))
        {
            return await _apiClient.GetUserFollowingsAsync(
                userId,
                cursor,
                count,
                cancellationToken
            );
        }

        // 查询当前登录用户的关注列表
        if (
            _authService.IsAuthenticated && !string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
        )
        {
            try
            {
                UserRelationPage remotePage = await _apiClient.GetUserFollowingsAsync(
                    _authService.CurrentUser.Id,
                    cursor,
                    count,
                    cancellationToken
                );

                if (string.IsNullOrEmpty(cursor) || cursor == "0")
                {
                    await _lock.WaitAsync(cancellationToken);
                    try
                    {
                        _followingIds.Clear();
                        foreach (UserRelationItem item in remotePage.Items)
                        {
                            if (!string.IsNullOrWhiteSpace(item.User?.Id))
                            {
                                _followingIds.Add(item.User.Id);
                            }
                        }

                        _followingsCache = remotePage.Items.ToList();
                        _cachedUserKey = GetCurrentUserKey();

                        string filePath = GetStorageFilePath("followings.json");
                        await WriteFileLockedAsync(filePath, _followingsCache, cancellationToken);
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }

                return remotePage;
            }
            catch (Exception ex)
            {
                _logger.ZLogWarning(ex, $"从服务端获取关注列表失败，降级本地缓存.");
            }
        }

        // 未登录或服务端请求失败时，从本地离线关注列表加载
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureFollowingsInitializedLockedAsync(cancellationToken);

            List<UserRelationItem> items = _followingsCache ?? [];
            int offset = 0;
            if (int.TryParse(cursor, out int parsedOffset))
            {
                offset = parsedOffset;
            }

            List<UserRelationItem> pageItems = items.Skip(offset).Take(count).ToList();
            int nextOffset = offset + pageItems.Count;
            bool hasMore = nextOffset < items.Count;

            return new UserRelationPage
            {
                Items = pageItems,
                Cursor = hasMore ? nextOffset.ToString() : string.Empty,
                HasMore = hasMore,
                IsPrivacyProtected = false,
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<UserRelationPage> GetFollowersAsync(
        string? userId = null,
        string cursor = "",
        int count = 20,
        CancellationToken cancellationToken = default
    )
    {
        bool isCurrentUser =
            string.IsNullOrWhiteSpace(userId)
            || (_authService.IsAuthenticated && _authService.CurrentUser?.Id == userId);

        // 如果查询的是其他用户，请求服务端接口
        if (!isCurrentUser && !string.IsNullOrWhiteSpace(userId))
        {
            return await _apiClient.GetUserFollowersAsync(userId, cursor, count, cancellationToken);
        }

        // 粉丝管理只有登录时需要：未登录账号无粉丝
        if (
            !_authService.IsAuthenticated || string.IsNullOrWhiteSpace(_authService.CurrentUser?.Id)
        )
        {
            return new UserRelationPage
            {
                Items = [],
                Cursor = string.Empty,
                HasMore = false,
                IsPrivacyProtected = false,
            };
        }

        try
        {
            UserRelationPage remotePage = await _apiClient.GetUserFollowersAsync(
                _authService.CurrentUser.Id,
                cursor,
                count,
                cancellationToken
            );

            if (string.IsNullOrEmpty(cursor) || cursor == "0")
            {
                string filePath = GetStorageFilePath("followers.json");
                await WriteFileLockedAsync(filePath, remotePage.Items.ToList(), cancellationToken);
            }

            return remotePage;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取粉丝列表失败.");

            // 失败时尝试读取本地粉丝缓存
            string filePath = GetStorageFilePath("followers.json");
            List<UserRelationItem> localFollowers = await ReadFileLockedAsync(
                filePath,
                cancellationToken
            );
            return new UserRelationPage
            {
                Items = localFollowers,
                Cursor = string.Empty,
                HasMore = false,
                IsPrivacyProtected = false,
            };
        }
    }

    private async Task<List<UserRelationItem>> ReadFileLockedAsync(
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

            return JsonAotSerializer.Deserialize<List<UserRelationItem>>(json) ?? [];
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"读取关注/粉丝数据文件失败: {filePath}");
            return [];
        }
    }

    private async Task WriteFileLockedAsync(
        string filePath,
        List<UserRelationItem> items,
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
