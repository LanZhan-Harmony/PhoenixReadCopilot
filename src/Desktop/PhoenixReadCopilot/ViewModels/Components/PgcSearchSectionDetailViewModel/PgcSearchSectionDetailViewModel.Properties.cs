using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Models.Search;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// PGC搜索分区详情视图模型.
/// </summary>
public sealed partial class PgcSearchSectionDetailViewModel
{
    private readonly ILogger<PgcSearchSectionDetailViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    private string _keyword = string.Empty;
    private SearchPartition? _partition;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

#pragma warning disable CS0067
    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler? ListUpdated;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public SearchSectionType SectionType { get; set; }

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<SeasonItemViewModel> Items { get; } = [];
}
