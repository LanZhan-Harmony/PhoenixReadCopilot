using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Items;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.User;

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
        IRelationshipService service,
        ILogger<FollowsPageViewModel> logger,
        DispatcherQueue dispatcherQueue,
        ISettingsService settingsService
    )
        : base(settingsService)
    {
        _service = service;
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
        await Task.Delay(200);
        string lastSelectedGroupId = _settings.FollowsPageLastSelectedGroupId;
        UserGroup group =
            Groups.FirstOrDefault(p => p.Id == lastSelectedGroupId)
            ?? Groups.FirstOrDefault(p => p.TotalCount > 0)
            ?? Groups.First();
        SelectGroupCommand.Execute(group);
        GroupInitialized?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void SelectGroup(UserGroup group)
    {
        if (group == null || group == SelectedGroup)
        {
            return;
        }

        if (Users.Count > 0)
        {
            _userCache[SelectedGroup] = Users.ToList();
        }

        SelectedGroup = group;
        _settings.FollowsPageLastSelectedGroupId = group.Id;

        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            async () =>
            {
                Users.Clear();
                if (_userCache.TryGetValue(group, out List<UserItemViewModel>? list))
                {
                    foreach (UserItemViewModel user in list)
                    {
                        Users.Add(user);
                    }

                    UserListUpdated?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    await LoadUsersAsync();
                }

                IsEmpty = Users.Count == 0;
            }
        );
    }

    [RelayCommand]
    private async Task LoadUsersAsync()
    {
        if (IsUserLoading || SelectedGroup is null)
        {
            return;
        }

        if (Users.Count >= SelectedGroup.TotalCount)
        {
            return;
        }

        IsUserLoading = true;
        try
        {
            int pn = 0;
            if (_offsetCache.TryGetValue(SelectedGroup, out int value))
            {
                pn = value;
            }

            (IReadOnlyList<UserCard>? users, int nextOffset) =
                await _service.GetMyFollowUserGroupDetailAsync(SelectedGroup.Id, pn);
            _offsetCache[SelectedGroup] = nextOffset;
            if (users != null)
            {
                foreach (UserCard item in users)
                {
                    Users.Add(new UserItemViewModel(item));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试加载关注用户时出错.");
        }
        finally
        {
            IsUserLoading = false;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        _dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            async () =>
            {
                Users.Clear();
                Groups.Clear();
                _userCache.Clear();
                _offsetCache.Clear();
                SelectedGroup = null;
                await InitializeAsync();
            }
        );
    }

    private async Task LoadGroupsAsync()
    {
        IsGroupLoading = true;
        try
        {
            IReadOnlyList<UserGroup> groups = await _service.GetMyFollowUserGroupsAsync();
            if (groups != null)
            {
                foreach (UserGroup item in groups)
                {
                    Groups.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "尝试加载关注分组时出错.");
        }
        finally
        {
            IsGroupLoading = false;
        }
    }
}
