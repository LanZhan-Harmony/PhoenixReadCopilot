using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Models.User;
using PhoenixReadCopilot.Pages.Overlay;
using PhoenixReadCopilot.ViewModels.Core;
using Windows.System;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 账户视图模型.
/// </summary>
public sealed partial class AccountViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountViewModel"/> class.
    /// </summary>
    public AccountViewModel(
        PhoenixReadApiClient apiClient,
        NavigationViewModel navService,
        AppViewModel appViewModel,
        ILogger<AccountViewModel> logger
    )
    {
        _apiClient = apiClient;
        _navService = navService;
        _appViewModel = appViewModel;
        _logger = logger;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (MyProfile is not null)
        {
            return;
        }

        IsInitializing = true;
        try
        {
            bool isValid = await _apiClient.ValidateSessionAsync();
            if (isValid)
            {
                CurrentUserInfo info = await _apiClient.GetCurrentUserInfoAsync();
                UserAvatar? avatar = !string.IsNullOrEmpty(info.AvatarUrl) && Uri.TryCreate(info.AvatarUrl, UriKind.Absolute, out Uri? uri)
                    ? new UserAvatar { Uri = uri }
                    : null;

                MyProfile = new UserDetailProfile
                {
                    User = new Models.User.User
                    {
                        Id = info.Id,
                        Name = string.IsNullOrEmpty(info.Name) ? "红果短剧用户" : info.Name,
                        Avatar = avatar,
                    },
                    Introduce = !string.IsNullOrEmpty(info.Description) ? info.Description : (info.IsVip ? "红果 VIP 会员" : "红果短剧用户"),
                    IsVip = info.IsVip,
                };
                Introduce = MyProfile.Introduce;
            }
            else
            {
                MyProfile = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取已登录用户信息失败，进入未登录状态");
            MyProfile = null;
        }
        finally
        {
            _appViewModel.IsInitialLoading = false;
            IsInitializing = false;
        }
    }

    [RelayCommand]
    private async Task UpdateCommunityInformationAsync()
    {
        if (MyProfile is null)
        {
            return;
        }

        try
        {
            DramaPage favs = await _apiClient.GetFavoritesAsync(count: 1);
            ReadHistoryPage history = await _apiClient.GetHistoryAsync(count: 1);
            FollowCount = (int)favs.Total;
            MomentCount = (int)history.Total;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取社区/收藏计数失败");
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        _apiClient.Logout();
        MyProfile = null;
        _appViewModel.RestartCommand.Execute(null);
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenPersonalWebsiteAsync()
    {
        if (MyProfile is null)
        {
            return;
        }

        await Launcher.LaunchUriAsync(new Uri($"https://hongguoduanju.com/user/{MyProfile.User.Id}"));
    }

    [RelayCommand]
    private void ShowFans()
    {
        if (MyProfile is not null)
        {
            _navService.NavigateToOver(typeof(FansPage), MyProfile.User.Id);
        }
    }

    [RelayCommand]
    private void ShowFollows() => ShowFavorites();

    [RelayCommand]
    private void ShowMoments()
    {
        if (MyProfile is not null)
        {
            _navService.NavigateToOver(typeof(UserSpacePage), MyProfile.User.Id);
        }
    }

    [RelayCommand]
    private void ShowViewLater() => _navService.NavigateToOver(typeof(ViewLaterPage));

    [RelayCommand]
    private void ShowHistory() => _navService.NavigateToOver(typeof(HistoryPage));

    [RelayCommand]
    private void ShowFavorites() => _navService.NavigateToOver(typeof(FavoritesPage));

    [RelayCommand]
    private static async Task UpdateUnreadAsync() => await Task.CompletedTask;
}
