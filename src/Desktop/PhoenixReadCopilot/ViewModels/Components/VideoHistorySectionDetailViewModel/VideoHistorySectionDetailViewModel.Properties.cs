using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 视频/短剧历史记录视图模型.
/// </summary>
public sealed partial class VideoHistorySectionDetailViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<VideoHistorySectionDetailViewModel> _logger;

    private bool _isPreventLoadMore;
    private int _offset;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <inheritdoc/>
    public ViewHistoryTabType Type => ViewHistoryTabType.Video;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Items { get; } = [];
}
