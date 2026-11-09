using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 视频分区页面视图模型.
/// </summary>
public sealed partial class VideoPartitionPageViewModel
{
    private readonly IVideoDiscoveryService _service;
    private readonly ILogger<VideoPartitionPageViewModel> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<Partition, VideoPartitionDetailViewModel> _partitionCache = [];

    [ObservableProperty]
    public partial bool IsPartitionLoading { get; set; }

    [ObservableProperty]
    public partial VideoPartitionDetailViewModel? SelectedPartition { get; set; }

    /// <summary>
    /// 区块加载完成.
    /// </summary>
    public event EventHandler PartitionInitialized;

    /// <summary>
    /// 主分区.
    /// </summary>
    public ObservableCollection<PartitionViewModel> Partitions { get; } = [];
}
