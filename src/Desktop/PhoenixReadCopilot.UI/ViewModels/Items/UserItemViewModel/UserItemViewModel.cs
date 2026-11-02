using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.Models;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Pages.Overlay;
using PhoenixReadCopilot.UI.Strings;
using Richasy.BiliKernel.Models.User;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Items;

/// <summary>
/// 用户项视图模型.
/// </summary>
public sealed partial class UserItemViewModel : ViewModelBase<UserCard>
{
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
        IsVip = data.Profile.IsVip ?? false;
        Level = data.Profile.Level;
    }

    [RelayCommand]
    private void ShowUserSpace() =>
        ItemServices.Navigation.NavigateToOver(typeof(UserSpacePage), Data.Profile.User);

    [RelayCommand]
    private void Pin()
    {
        var pinItem = new PinItem(
            Data.Profile.User.Id,
            Data.Profile.User.Name,
            Data.Profile.User.Avatar.Uri.ToString(),
            PinContentType.User
        );
        ItemServices.Pinner.AddItemCommand.Execute(pinItem);
    }

    [RelayCommand]
    private async Task ToggleFollowAsync()
    {
        var relationService = ItemServices.Relationship;
        if (IsFollowed)
        {
            try
            {
                await relationService.UnfollowUserAsync(Id);
                IsFollowed = false;
                ItemServices.App
                    .ShowTipCommand.Execute((Resources.Unfollowed, InfoType.Success));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "取消关注用户时失败");
                ItemServices.App
                    .ShowTipCommand.Execute((Resources.FailedToUnfollowUser, InfoType.Error));
            }
        }
        else
        {
            try
            {
                await relationService.FollowUserAsync(Id);
                IsFollowed = true;
                ItemServices.App
                    .ShowTipCommand.Execute((Resources.Followed, InfoType.Success));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "关注用户时失败");
                ItemServices.App
                    .ShowTipCommand.Execute((Resources.FailedToFollowUser, InfoType.Error));
            }
        }
    }
}
