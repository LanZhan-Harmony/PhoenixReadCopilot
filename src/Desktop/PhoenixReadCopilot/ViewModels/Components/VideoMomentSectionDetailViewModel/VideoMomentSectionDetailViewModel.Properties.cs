using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Models.Settings;
using PhoenixReadCopilot.ViewModels.Items;
using PhoenixReadCopilot.ViewModels.View;
using Richasy.BiliKernel.Bili.Moment;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频动态分区详情视图模型.
/// </summary>
public sealed partial class VideoMomentSectionDetailViewModel
{
    private readonly IMomentDiscoveryService _service;
    private readonly ILogger<VideoMomentSectionDetailViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly MomentPageViewModel _momentPageViewModel;
    private readonly AppSettings _settings;

    private bool _preventLoadMore;
    private string? _offset;
    private string? _baseline;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <inheritdoc/>
    public MomentSectionType SectionType => MomentSectionType.Video;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<MomentItemViewModel> Items { get; } = new();
}
