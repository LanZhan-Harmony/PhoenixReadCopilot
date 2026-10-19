using System.Collections.ObjectModel;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.ViewModels.Items;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Richasy.BiliKernel.Bili.Moment;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 视频动态分区详情视图模型.
/// </summary>
public sealed partial class VideoMomentSectionDetailViewModel
{
    private readonly IMomentDiscoveryService _service;
    private readonly ILogger<VideoMomentSectionDetailViewModel> _logger;

    private bool _preventLoadMore;
    private string? _offset;
    private string? _baseline;

    [ObservableProperty] private bool _isEmpty;

    [ObservableProperty] private bool _isLoading;

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
