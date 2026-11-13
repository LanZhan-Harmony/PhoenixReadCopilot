using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using ZLogger;
using PhoenixReadCopilot.Core.Contracts.Services;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 关注页面视图模型.
/// </summary>
public sealed partial class FollowsPageViewModel : LayoutPageViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FollowsPageViewModel"/> class.
    /// </summary>
    public FollowsPageViewModel(
        ILogger<FollowsPageViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
    }

    /// <inheritdoc/>
    protected override string GetPageKey() => nameof(FollowsPage);

    [RelayCommand]
    private async Task InitializeAsync()
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
            SelectGroupCommand.Execute(group);
        }

        GroupInitialized?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SelectGroup(UserGroup group)
    {
        if (group is null || group == SelectedGroup)
        {
            return;
        }

        SelectedGroup = group;
        _settings.FollowsPageLastSelectedGroupId = group.Id;

        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
        {
            Users.Clear();
            await LoadUsersAsync();
            IsEmpty = Users.Count == 0;
        });
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        if (IsUserLoading || SelectedGroup is null)
        {
            return;
        }

        IsUserLoading = true;
        try
        {
            // TODO: 红果关注用户列表 API 暂未开放
            await Task.CompletedTask;
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"尝试加载关注用户时出错.");
        }
        finally
        {
            IsUserLoading = false;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () =>
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
