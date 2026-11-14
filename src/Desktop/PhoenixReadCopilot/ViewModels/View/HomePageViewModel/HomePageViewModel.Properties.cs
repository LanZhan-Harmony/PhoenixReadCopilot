using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 首页视图模型 (属性分部).
/// </summary>
public sealed partial class HomePageViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<HomePageViewModel> _logger;
    private readonly NavigationViewModel _navigationViewModel;
    private readonly DispatcherQueue _dispatcherQueue;

    private int _recommendOffset;
    private string? _recommendSessionId;
    private readonly HashSet<string> _seenDramaIds = [];
    private bool _hasMoreRecommendations = true;

    private long _currentRequestId;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// 是否正在加载推荐.
    /// </summary>
    [ObservableProperty]
    public partial bool IsVideoLoading { get; set; }

    /// <summary>
    /// 推荐视频列表已更新事件.
    /// </summary>
    public event EventHandler? VideoListUpdated;

    /// <summary>
    /// 推荐短剧列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Videos { get; } = [];
}
