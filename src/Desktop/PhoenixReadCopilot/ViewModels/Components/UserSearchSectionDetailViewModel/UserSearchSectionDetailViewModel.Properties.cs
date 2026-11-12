using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Search;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 用户搜索分区详情视图模型.
/// </summary>
public sealed partial class UserSearchSectionDetailViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<UserSearchSectionDetailViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    private bool _canRequest;
    private int _offset;
    private string _keyword = string.Empty;
    private SearchPartition? _partition;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler? ListUpdated;

    /// <inheritdoc/>
    public SearchSectionType SectionType => SearchSectionType.User;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<UserItemViewModel> Items { get; } = [];
}
