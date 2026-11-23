using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Pages;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 制作人员项视图模型.
/// </summary>
internal sealed partial class DramaCreditItemViewModel : ViewModelBase<DramaCredit>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<DramaCreditItemViewModel> _logger;

    public DramaCreditItemViewModel(
        DramaCredit credit,
        INotificationService? notificationService = null,
        ILogger<DramaCreditItemViewModel>? logger = null
    )
        : base(credit)
    {
        _notificationService = notificationService ?? ItemServices.Notification;
        _logger = logger
            ?? CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<
                ILogger<DramaCreditItemViewModel>
            >();

        User = credit.User;
        Name = credit.User?.Name ?? string.Empty;

        if (
            !string.IsNullOrEmpty(credit.User?.AvatarUrl)
            && Uri.TryCreate(credit.User.AvatarUrl, UriKind.Absolute, out Uri? avatarUri)
        )
        {
            Avatar = avatarUri;
        }

        if (credit.Roles is { Count: > 0 })
        {
            IEnumerable<string> roleTexts = credit.Roles.Select(r => r.Label);
            RoleText = string.Join(" / ", roleTexts.Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        if (string.IsNullOrWhiteSpace(RoleText))
        {
            RoleText = "参演";
        }

        IsFollowed =
            User is not null
            && !string.IsNullOrEmpty(User.Id)
            && ItemServices.Follow.IsFollowing(User.Id);
    }

    public User User { get; }

    public string Name { get; }

    public Uri? Avatar { get; }

    public string RoleText { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FollowButtonText))]
    public partial bool IsFollowed { get; set; }

    public string FollowButtonText => IsFollowed ? "已关注" : "关注";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [RelayCommand]
    public void ShowUserSpace()
    {
        if (Data is not null)
        {
            ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Data);
        }
        else if (User is not null && !string.IsNullOrEmpty(User.Id))
        {
            ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), User.Id);
        }
    }

    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        if (User is null || string.IsNullOrEmpty(User.Id))
        {
            return;
        }

        IsLoading = true;
        try
        {
            bool next = !IsFollowed;
            PhoenixOperationResult res = await ItemServices.Follow.SetFollowAsync(
                User.Id,
                next,
                User
            );
            if (res.Succeeded)
            {
                IsFollowed = next;
                _notificationService.ShowInAppNotification(next ? "关注成功" : "已取消关注", NotificationType.Success);
            }
            else
            {
                _notificationService.ShowInAppNotification(res.Message ?? "操作失败", NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"关注用户操作异常.");
            _notificationService.ShowInAppNotification("关注操作失败", NotificationType.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
