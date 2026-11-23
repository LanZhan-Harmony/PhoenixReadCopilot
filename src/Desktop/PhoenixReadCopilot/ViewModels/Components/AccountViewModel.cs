using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.Pages;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 账户视图模型.
/// </summary>
internal sealed partial class AccountViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authService;
    private readonly IHistoryService _historyService;
    private readonly IFavoriteService _favoriteService;
    private readonly IFollowService _followService;
    private readonly INavigationService _navService;
    private readonly ILogger<AccountViewModel> _logger;
    private readonly AppViewModel _appViewModel;

    [ObservableProperty]
    public partial UserDetailProfile? MyProfile { get; set; }

    [ObservableProperty]
    public partial bool IsInitializing { get; set; }

    [ObservableProperty]
    public partial int FollowCount { get; set; }

    [ObservableProperty]
    public partial int FansCount { get; set; }

    [ObservableProperty]
    public partial string Introduce { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountViewModel"/> class.
    /// </summary>
    public AccountViewModel(
        IAuthenticationService authService,
        IHistoryService historyService,
        IFavoriteService favoriteService,
        IFollowService followService,
        INavigationService navService,
        AppViewModel appViewModel,
        ILogger<AccountViewModel> logger
    )
    {
        _authService = authService;
        _historyService = historyService;
        _favoriteService = favoriteService;
        _followService = followService;
        _navService = navService;
        _appViewModel = appViewModel;
        _logger = logger;

        _authService.StateChanged += OnAuthStateChanged;
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        if (!isAuthenticated)
        {
            MyProfile = null;
            FollowCount = 0;
            FansCount = 0;
            Introduce = string.Empty;
        }
        else
        {
            _ = InitializeAsync();
        }
    }

    public async Task InitializeAsync()
    {
        if (MyProfile is not null)
        {
            return;
        }

        IsInitializing = true;
        try
        {
            bool isValid = await _authService.InitializeAsync();
            if (isValid && _authService.CurrentUser is { } info)
            {
                UserAvatar? avatar =
                    !string.IsNullOrEmpty(info.AvatarUrl)
                    && Uri.TryCreate(info.AvatarUrl, UriKind.Absolute, out Uri? uri)
                        ? new UserAvatar { Uri = uri }
                        : null;

                MyProfile = new UserDetailProfile
                {
                    User = new PhoenixReadCopilot.Core.Models.User.User
                    {
                        Id = info.Id,
                        Name = string.IsNullOrEmpty(info.Name) ? "红果短剧用户" : info.Name,
                        Avatar = avatar,
                    },
                    Introduce = !string.IsNullOrEmpty(info.Description)
                        ? info.Description
                        : (info.IsVip ? "红果 VIP 会员" : "红果短剧用户"),
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
            _logger.ZLogWarning(ex, $"获取已登录用户信息失败，进入未登录状态");
            MyProfile = null;
        }
        finally
        {
            _appViewModel.IsInitialLoading = false;
            IsInitializing = false;
        }
    }

    public async Task UpdateCommunityInformationAsync()
    {
        if (MyProfile is null)
        {
            return;
        }

        try
        {
            DramaPage favs = await _favoriteService.GetFavoritesAsync(count: 1);
            ReadHistoryPage history = await _historyService.GetHistoryAsync(count: 1);
            UserRelationPage followings = await _followService.GetFollowingsAsync(count: 1);
            FollowCount = followings.Items.Count;

            if (_authService.IsAuthenticated)
            {
                UserRelationPage followers = await _followService.GetFollowersAsync(count: 1);
                FansCount = followers.Items.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"获取社区/收藏计数失败");
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _authService.LogoutAsync();
        MyProfile = null;
    }

    [RelayCommand]
    private void ShowFollows() => _navService.NavigateTo(typeof(UserSpacePage), "Follows");

    [RelayCommand]
    private void ShowFans() => _navService.NavigateTo(typeof(UserSpacePage), "Fans");

    [RelayCommand]
    private void ShowViewLater() => _navService.NavigateTo(typeof(UserSpacePage), "ViewLater");

    [RelayCommand]
    private void ShowHistory() => _navService.NavigateTo(typeof(UserSpacePage), "History");

    [RelayCommand]
    private void ShowFavorite() => _navService.NavigateTo(typeof(UserSpacePage), "Favorite");
}
