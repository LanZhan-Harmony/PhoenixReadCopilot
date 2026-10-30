using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// UGC合集详情视图模型.
/// </summary>
public sealed partial class UgcSeasonFavoriteSectionDetailViewModel
{
    private readonly IFavoriteService _service;
    private readonly ILogger<UgcSeasonFavoriteSectionDetailViewModel> _logger;

    private int _pageNumber;
    private bool _preventLoadMore;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial Uri? Cover { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<VideoItemViewModel> Items { get; } = new();
}
