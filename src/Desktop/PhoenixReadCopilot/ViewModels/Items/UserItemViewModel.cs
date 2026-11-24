using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models.Pin;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.Strings;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 用户项视图模型.
/// </summary>
internal sealed partial class UserItemViewModel : ViewModelBase
{
    private static readonly ILogger<UserItemViewModel> _logger = Ioc.Default.GetRequiredService<
        ILogger<UserItemViewModel>
    >();

    /// <summary>
    /// 用户数据实体.
    /// </summary>
    public UserCard Data { get; }

    [ObservableProperty]
    public partial bool IsFollowed { get; set; }

    /// <summary>
    /// 用户名.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 用户描述.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// 用户头像.
    /// </summary>
    public Uri? Avatar { get; init; }

    /// <summary>
    /// 用户 Id.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// 红果用户数据.
    /// </summary>
    public PhoenixReadApi.User? UserData { get; }

    public UserItemViewModel(UserCard data)
    {
        Data = data;
        Id = data.Profile.User.Id;
        Name = data.Profile.User.Name;
        Avatar = data.Profile.User.Avatar?.Uri;
        Description = string.IsNullOrEmpty(data.Profile.Introduce)
            ? Resources.NoSelfIntroduce
            : data.Profile.Introduce;
        IsFollowed = ItemServices.Follow.IsFollowing(Id);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserItemViewModel"/> class from <see cref="PhoenixReadApi.User"/>.
    /// </summary>
    public UserItemViewModel(PhoenixReadApi.User user)
    {
        Data = CreateUserCard(user);
        UserData = user;
        Id = user.Id;
        Name = user.Name;
        if (
            !string.IsNullOrEmpty(user.AvatarUrl)
            && Uri.TryCreate(user.AvatarUrl, UriKind.Absolute, out Uri? uri)
        )
        {
            Avatar = uri;
        }
        Description = string.IsNullOrEmpty(user.Introduction)
            ? Resources.NoSelfIntroduce
            : user.Introduction;
        IsFollowed = ItemServices.Follow.IsFollowing(Id);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserItemViewModel"/> class from <see cref="UserRelationItem"/>.
    /// </summary>
    public UserItemViewModel(UserRelationItem item)
        : this(item.User)
    {
        IsFollowed = item.IsFollowing || ItemServices.Follow.IsFollowing(Id);
    }

    private static UserCard CreateUserCard(PhoenixReadApi.User user)
    {
        UserAvatar? avatar =
            !string.IsNullOrEmpty(user.AvatarUrl)
            && Uri.TryCreate(user.AvatarUrl, UriKind.Absolute, out Uri? uri)
                ? new UserAvatar { Uri = uri }
                : null;

        return new UserCard
        {
            Profile = new UserDetailProfile
            {
                User = new PhoenixReadCopilot.Core.Models.User.User
                {
                    Id = user.Id,
                    Name = user.Name,
                    Avatar = avatar,
                },
                Introduce = user.Introduction,
            },
            Community = new UserCommunity(),
        };
    }

    [RelayCommand]
    private void ShowUserSpace() =>
        ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Id);

    [RelayCommand]
    private void Pin()
    {
        string coverUri = Avatar?.ToString() ?? string.Empty;
        PinItem pinItem = new(Id, Name, coverUri, PinType.User);
        ItemServices.Pinner.AddItem(pinItem);
    }

    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        try
        {
            bool next = !IsFollowed;
            await ItemServices.Follow.SetFollowAsync(Id, next, UserData);
            IsFollowed = next;
            ItemServices.Notification.ShowInAppNotification(
                IsFollowed ? Resources.Followed : Resources.Unfollowed,
                NotificationType.Success
            );
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"修改关注状态失败");
            ItemServices.Notification.ShowInAppNotification(
                Resources.FailedToFollowUser,
                NotificationType.Error
            );
        }
    }
}
