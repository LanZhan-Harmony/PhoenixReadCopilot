using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Items;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 关注页面视图模型.
/// </summary>
public sealed partial class FollowsPageViewModel : LayoutPageViewModelBase
{
    private readonly IFollowService _followService;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="FollowsPageViewModel"/> class.
    /// </summary>
    public FollowsPageViewModel(
        IFollowService followService,
        ILogger<FollowsPageViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _followService = followService;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(FollowsPage);

    public async Task InitializeAsync()
    {
        if (Groups.Count > 0)
        {
            return;
        }

        await LoadGroupsAsync();
        await Task.Delay(50);
        UserGroup? group = Groups.FirstOrDefault();
        if (group is not null)
        {
            SelectGroup(group);
        }

        GroupInitialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 选择分组.
    /// </summary>
    public void SelectGroup(UserGroup group)
    {
        if (group is null || group == SelectedGroup)
        {
            return;
        }

        SelectedGroup = group;
        _settings.FollowsPageLastSelectedGroupId = group.Id;

        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
            async () =>
            {
                Users.Clear();
                await LoadUsersAsync();
                IsEmpty = Users.Count == 0;
            });
    }

    public async Task LoadUsersAsync()
    {
        if (IsUserLoading || SelectedGroup is null)
        {
            return;
        }

        IsUserLoading = true;
        try
        {
            UserRelationPage page = await _followService.GetFollowingsAsync();
            Users.Clear();
            foreach (UserRelationItem item in page.Items)
            {
                Users.Add(new UserItemViewModel(item));
            }
            IsEmpty = Users.Count == 0;
            UserListUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载关注用户时出错.");
            IsEmpty = true;
        }
        finally
        {
            IsUserLoading = false;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,
            async () =>
            {
                Users.Clear();
                Groups.Clear();
                _userCache.Clear();
                _offsetCache.Clear();
                SelectedGroup = null;
                await InitializeAsync();
            });
    }

    private async Task LoadGroupsAsync()
    {
        IsGroupLoading = true;
        try
        {
            // TODO: 红果关注分组 API 暂未开放
            Groups.Clear();
            Groups.Add(new UserGroup("default", "全部关注", 0));
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载关注分组时出错.");
        }
        finally
        {
            IsGroupLoading = false;
        }
    }
}
