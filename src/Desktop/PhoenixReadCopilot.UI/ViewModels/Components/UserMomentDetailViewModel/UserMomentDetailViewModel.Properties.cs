using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Bili.Moment;
using Richasy.BiliKernel.Models.User;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 用户动态详情视图模型.
/// </summary>
public sealed partial class UserMomentDetailViewModel
{
    private readonly IMomentDiscoveryService _service;
    private readonly ILogger<UserMomentDetailViewModel> _logger;
    private bool _isVideo;
    private bool _preventLoadMore;
    private string? _offset;
    private UserProfile _user;
    private Action<MomentItemViewModel> _showCommentAction;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<MomentItemViewModel> Items { get; set; } = new();

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler ListUpdated;
}
