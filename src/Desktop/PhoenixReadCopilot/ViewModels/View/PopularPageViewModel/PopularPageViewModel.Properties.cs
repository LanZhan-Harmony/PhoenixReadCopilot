using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 流行视频页面视图模型.
/// </summary>
public sealed partial class PopularPageViewModel
{
    private readonly IVideoDiscoveryService _service;
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<PopularPageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Dictionary<
        IPopularSectionItemViewModel,
        List<VideoItemViewModel>
    > _videoCache = new();

    private int _recommendOffset;
    private string? _recommendSessionId;
    private readonly HashSet<string> _seenDramaIds = new();
    private bool _hasMoreRecommendations = true;
    private CancellationTokenSource? _cancellationTokenSource;
    private long _hotOffset;

    [ObservableProperty]
    public partial bool IsPartitionLoading { get; set; }

    [ObservableProperty]
    public partial bool IsVideoLoading { get; set; }

    [ObservableProperty]
    public partial IPopularSectionItemViewModel SelectedSection { get; set; }

    /// <summary>
    /// 区块加载完成.
    /// </summary>
    public event EventHandler SectionInitialized;

    /// <summary>
    /// 视频列表已完成更新.
    /// </summary>
    public event EventHandler VideoListUpdated;

    /// <summary>
    /// 分区列表.
    /// </summary>
    public ObservableCollection<IPopularSectionItemViewModel> Sections { get; } = new();

    /// <summary>
    /// 视频列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Videos { get; } = new();
}
