using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Strings;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频播放器推荐区域详情视图模型（支持后台异步加载与增量填充）.
/// </summary>
internal sealed partial class VideoPlayerRecommendSectionDetailViewModel(
    IDramaService dramaService,
    DispatcherQueue dispatcherQueue,
    ILogger logger
) : ViewModelBase, IPlayerSectionDetailViewModel
{
    private readonly IDramaService _dramaService = dramaService;
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue;
    private readonly ILogger _logger = logger;

    private string _dramaId = string.Empty;
    private Task? _loadingTask;
    private CancellationTokenSource? _loadCts;

    [ObservableProperty]
    public partial ObservableCollection<DramaItemViewModel> Items { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <inheritdoc/>
    public string Title { get; } = Resources.Recommend;

    /// <summary>
    /// 初始化推荐区块并重置旧数据.
    /// </summary>
    public void Initialize(string dramaId)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
        _loadingTask = null;
        _dramaId = dramaId;
        IsLoading = false;
        Items.Clear();
    }

    /// <summary>
    /// 在后台启动关联推荐加载（不阻塞首屏）.
    /// </summary>
    public void StartBackgroundLoad(CancellationToken externalToken = default)
    {
        if (string.IsNullOrEmpty(_dramaId) || Items.Count > 0 || _loadingTask is not null)
        {
            return;
        }

        _loadCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        _loadingTask = LoadCoreAsync(_dramaId, _loadCts.Token);
    }

    public async Task TryFirstLoadAsync()
    {
        if (Items.Count > 0 || string.IsNullOrEmpty(_dramaId))
        {
            return;
        }

        if (_loadingTask is not null)
        {
            await _loadingTask;
            return;
        }

        StartBackgroundLoad();
        if (_loadingTask is not null)
        {
            await _loadingTask;
        }
    }

    private async Task LoadCoreAsync(string dramaId, CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            DramaRecommendations rec = await _dramaService.GetDramaRecommendationsAsync(
                dramaId,
                cancellationToken
            );

            if (cancellationToken.IsCancellationRequested || dramaId != _dramaId)
            {
                return;
            }

            if (rec?.Items is { Count: > 0 } recItems)
            {
                List<DramaItemViewModel> vms =
                [
                    .. recItems.Select(d => new DramaItemViewModel(d, VideoCardStyle.PlayerRecommend)),
                ];

                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (cancellationToken.IsCancellationRequested || dramaId != _dramaId)
                    {
                        return;
                    }

                    Items.Clear();
                    foreach (DramaItemViewModel vm in vms)
                    {
                        Items.Add(vm);
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取推荐好剧失败.");
        }
        finally
        {
            if (dramaId == _dramaId)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (dramaId == _dramaId)
                    {
                        IsLoading = false;
                    }
                });
            }
        }
    }
}
