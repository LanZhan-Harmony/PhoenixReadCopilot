using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 分类筛选服务实现，提供多维筛选维度面板与短剧检索.
/// </summary>
public sealed class FilterService : IFilterService
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<FilterService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<ShortDramaGenre, IReadOnlyList<ShortDramaFilterGroup>> _panelCache = [];

    public FilterService(PhoenixReadApiClient apiClient, ILogger<FilterService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ShortDramaFilterGroup>> GetFilterPanelAsync(
        ShortDramaGenre genre = ShortDramaGenre.ShortPlay,
        CancellationToken cancellationToken = default
    )
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_panelCache.TryGetValue(genre, out IReadOnlyList<ShortDramaFilterGroup>? cached))
            {
                return cached;
            }

            IReadOnlyList<ShortDramaFilterGroup> panel =
                await _apiClient.GetShortDramaFilterPanelAsync(genre, cancellationToken);
            _panelCache[genre] = panel;
            return panel;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取短剧筛选维度面板失败: genre={genre}");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ShortDramaFilterPage> FilterShortDramasAsync(
        ShortDramaFilterQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            return await _apiClient.FilterShortDramasAsync(query, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(
                ex,
                $"筛选短剧失败: genre={query.Genre}, offset={query.Offset}"
            );
            throw;
        }
    }
}
