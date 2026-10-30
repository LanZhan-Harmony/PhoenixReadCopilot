using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 视频收藏分区详情视图模型.
/// </summary>
public sealed partial class VideoFavoriteSectionDetailViewModel
{
    private readonly IFavoriteService _service;
    private readonly ILogger<VideoFavoriteSectionDetailViewModel> _logger;

    private int _pageNumber;
    private bool _preventLoadMore;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Items { get; } = new();
}
