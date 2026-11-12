using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 关注页面视图模型.
/// </summary>
public sealed partial class FollowsPageViewModel
{
    private readonly ILogger<FollowsPageViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Dictionary<UserGroup, List<UserItemViewModel>> _userCache = [];
    private readonly Dictionary<UserGroup, int> _offsetCache = [];

    [ObservableProperty]
    public partial bool IsGroupLoading { get; set; }

    [ObservableProperty]
    public partial bool IsUserLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial UserGroup? SelectedGroup { get; set; }

    /// <summary>
    /// 分组加载完成.
    /// </summary>
    public event EventHandler? GroupInitialized;

    /// <summary>
    /// 用户列表已完成更新.
    /// </summary>
    public event EventHandler? UserListUpdated;

    /// <summary>
    /// 分组.
    /// </summary>
    public ObservableCollection<UserGroup> Groups { get; } = [];

    /// <summary>
    /// 显示的用户列表.
    /// </summary>
    public ObservableCollection<UserItemViewModel> Users { get; } = [];
}
