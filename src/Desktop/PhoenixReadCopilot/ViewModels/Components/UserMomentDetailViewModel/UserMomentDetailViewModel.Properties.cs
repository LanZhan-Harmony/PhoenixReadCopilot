using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 用户动态/作品详情视图模型.
/// </summary>
public sealed partial class UserMomentDetailViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<UserMomentDetailViewModel> _logger;
    private bool _isVideo;
    private bool _preventLoadMore;
    private string? _offset;
    private string _userId = string.Empty;
    private Action<MomentItemViewModel>? _showCommentAction;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<MomentItemViewModel> Items { get; set; } = new();

    /// <summary>
    /// 列表已完成更新.
    /// </summary>
    public event EventHandler? ListUpdated;
}
