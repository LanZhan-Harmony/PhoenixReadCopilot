using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.Search;
using Richasy.BiliKernel.Models.Search;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 用户搜索分区详情视图模型.
/// </summary>
public sealed partial class UserSearchSectionDetailViewModel
{
    private readonly ISearchService _service;
    private readonly ILogger<UserSearchSectionDetailViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    private bool _canRequest;
    private string _offset;
    private string _keyword;
    private SearchPartition _partition;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;

    /// <inheritdoc/>
    public SearchSectionType SectionType => SearchSectionType.User;

    /// <summary>
    /// 条目列表.
    /// </summary>
    public ObservableCollection<UserItemViewModel> Items { get; } = [];
}
