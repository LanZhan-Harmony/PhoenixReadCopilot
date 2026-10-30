using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 视频分区详情视图模型.
/// </summary>
public sealed partial class VideoPartitionDetailViewModel
{
    private readonly ILogger<VideoPartitionDetailViewModel> _logger;
    private readonly IVideoDiscoveryService _service;
    private readonly Dictionary<string, List<VideoItemViewModel>> _childPartitionVideoCache = new();
    private readonly Dictionary<string, (long Offset, int PageNumber)> _childPartitionOffsetCache =
        new();

    private List<VideoItemViewModel> _recommendVideoCache;
    private long _recommendOffset;

    [ObservableProperty]
    public partial List<PartitionVideoSortType> SortTypes { get; set; }

    [ObservableProperty]
    public partial List<PartitionViewModel> Children { get; set; }

    [ObservableProperty]
    public partial PartitionVideoSortType SelectedSortType { get; set; }

    [ObservableProperty]
    public partial bool IsRecommend { get; set; }

    [ObservableProperty]
    public partial bool IsVideoLoading { get; set; }

    [ObservableProperty]
    public partial PartitionViewModel CurrentPartition { get; set; }

    /// <summary>
    /// 初始化完成.
    /// </summary>
    public event EventHandler Initialized;

    /// <summary>
    /// 视频列表已完成更新.
    /// </summary>
    public event EventHandler VideoListUpdated;

    /// <summary>
    /// 显示的视频列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Videos { get; } = new();
}
