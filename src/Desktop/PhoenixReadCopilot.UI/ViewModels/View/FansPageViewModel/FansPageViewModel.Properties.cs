using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 粉丝页面视图模型.
/// </summary>
public sealed partial class FansPageViewModel
{
    private readonly IRelationshipService _service;
    private readonly ILogger<FansPageViewModel> _logger;

    private int _pageNumber;

    [ObservableProperty]
    public partial bool IsUserLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    /// <summary>
    /// 用户列表已完成更新.
    /// </summary>
    public event EventHandler UserListUpdated;

    /// <summary>
    /// 用户列表.
    /// </summary>
    public ObservableCollection<UserItemViewModel> Users { get; } = new();
}
