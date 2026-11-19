using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 粉丝页面视图模型.
/// </summary>
public sealed partial class FansPageViewModel : ViewModelBase
{
    private readonly IFollowService _followService;
    private readonly ILogger<FansPageViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    [ObservableProperty]
    public partial bool IsUserLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    /// <summary>
    /// 用户列表已完成更新.
    /// </summary>
    public event EventHandler? UserListUpdated;

    /// <summary>
    /// 用户列表.
    /// </summary>
    public ObservableCollection<UserItemViewModel> Users { get; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="FansPageViewModel"/> class.
    /// </summary>
    public FansPageViewModel(
        IFollowService followService,
        ILogger<FansPageViewModel> logger,
        DispatcherQueue dispatcherQueue
    )
    {
        _followService = followService;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
    }

    public async Task InitializeAsync()
    {
        if (Users.Count > 0 || IsEmpty)
        {
            return;
        }

        await LoadUsersAsync();
    }

    [RelayCommand]
    private void Refresh()
    {
        IsEmpty = false;
        TotalCount = 0;
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
            async () =>
            {
                Users.Clear();
                await InitializeAsync();
            });
    }

    public async Task LoadUsersAsync()
    {
        if (IsUserLoading || IsEmpty)
        {
            return;
        }

        IsUserLoading = true;
        try
        {
            UserRelationPage page = await _followService.GetFollowersAsync();
            Users.Clear();
            foreach (UserRelationItem item in page.Items)
            {
                Users.Add(new UserItemViewModel(item));
            }
            TotalCount = Users.Count;
            IsEmpty = Users.Count == 0;
            UserListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"加载粉丝列表失败");
            IsEmpty = true;
        }
        finally
        {
            IsUserLoading = false;
        }
    }
}
