using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.Strings;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.Appearance;
using Richasy.BiliKernel.Models.User;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 用户项视图模型.
/// </summary>
public sealed partial class UserItemViewModel : ViewModelBase<UserCard>
{
    /// <summary>
    /// 红果用户数据.
    /// </summary>
    public PhoenixReadApi.User? UserData { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserItemViewModel"/> class.
    /// </summary>
    public UserItemViewModel(UserCard data)
        : base(data)
    {
        Id = data.Profile.User.Id;
        Name = data.Profile.User.Name;
        Avatar = data.Profile.User.Avatar.Uri;
        Description = string.IsNullOrEmpty(data.Profile.Introduce)
            ? Resources.NoSelfIntroduce
            : data.Profile.Introduce;
        IsFollowed =
            data.Community.Relation != UserRelationStatus.Unknown
            && data.Community.Relation != UserRelationStatus.Unfollow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserItemViewModel"/> class from <see cref="PhoenixReadApi.User"/>.
    /// </summary>
    public UserItemViewModel(PhoenixReadApi.User user)
        : base(CreateUserCard(user))
    {
        UserData = user;
        Id = user.Id;
        Name = user.Name;
        if (!string.IsNullOrEmpty(user.AvatarUrl) && Uri.TryCreate(user.AvatarUrl, UriKind.Absolute, out Uri? uri))
        {
            Avatar = uri;
        }
        Description = string.IsNullOrEmpty(user.Introduction)
            ? Resources.NoSelfIntroduce
            : user.Introduction;
        IsFollowed = false;
    }

    private static UserCard CreateUserCard(PhoenixReadApi.User user)
    {
        BiliImage? avatar = !string.IsNullOrEmpty(user.AvatarUrl) && Uri.TryCreate(user.AvatarUrl, UriKind.Absolute, out Uri? uri)
            ? new BiliImage(uri)
            : null;
        Richasy.BiliKernel.Models.User.UserProfile userProfile = new(user.Id, user.Name, avatar);
        Richasy.BiliKernel.Models.User.UserDetailProfile detailProfile = new(userProfile, user.Introduction, null, null, null);
        return new UserCard(detailProfile, new UserCommunityInformation(user.Id));
    }

    [RelayCommand]
    private void ShowUserSpace() =>
        ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Id);

    [RelayCommand]
    private void Pin()
    {
        string coverUri = Avatar?.ToString() ?? string.Empty;
        PinItem pinItem = new(
            Id,
            Name,
            coverUri,
            PinContentType.User
        );
        ItemServices.Pinner.AddItemCommand.Execute(pinItem);
    }

    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        try
        {
            await ItemServices.PhoenixRead.SetFollowAsync(Id, !IsFollowed);
            IsFollowed = !IsFollowed;
            ItemServices.App.ShowTipCommand.Execute(
                (IsFollowed ? Resources.Followed : Resources.Unfollowed, InfoType.Success)
            );
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "修改关注状态失败");
            ItemServices.App.ShowTipCommand.Execute(
                (Resources.FailedToFollowUser, InfoType.Error)
            );
        }
    }
}
