using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 追番分区详情视图模型.
/// </summary>
public sealed partial class PgcFavoriteSectionDetailViewModel
{
    private readonly IFavoriteService _service;
    private readonly ILogger<PgcFavoriteSectionDetailViewModel> _logger;
    private readonly Dictionary<PgcFavoriteStatus, List<SeasonItemViewModel>> _cache = new();
    private readonly Dictionary<PgcFavoriteStatus, (int, bool)> _offsetCache = new();

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial PgcFavoriteStatus CurrentStatus { get; set; }

    [ObservableProperty]
    public partial List<PgcFavoriteStatus> StatusList { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<SeasonItemViewModel> Items { get; } = new();

    /// <summary>
    /// 类型.
    /// </summary>
    public PgcFavoriteType Type { get; }
}
