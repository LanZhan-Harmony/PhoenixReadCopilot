using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.UI.ViewModels.Core;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Items;

/// <summary>
/// 制作人员项视图模型.
/// </summary>
public sealed partial class DramaCreditItemViewModel : ViewModelBase<DramaCredit>
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly AppViewModel _appViewModel;
    private readonly ILogger<DramaCreditItemViewModel> _logger;

    public DramaCreditItemViewModel(
        DramaCredit credit,
        PhoenixReadApiClient apiClient,
        AppViewModel? appViewModel = null,
        ILogger<DramaCreditItemViewModel>? logger = null
    )
        : base(credit)
    {
        _apiClient = apiClient;
        _appViewModel = appViewModel ?? ItemServices.App;
        _logger =
            logger
            ?? CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<
                ILogger<DramaCreditItemViewModel>
            >();

        User = credit.User;
        Name = credit.User?.Name ?? string.Empty;

        if (
            !string.IsNullOrEmpty(credit.User?.AvatarUrl)
            && Uri.TryCreate(credit.User.AvatarUrl, UriKind.Absolute, out var avatarUri)
        )
        {
            Avatar = avatarUri;
        }

        if (credit.Roles is { Count: > 0 })
        {
            var roleTexts = credit.Roles.Select(r => r.Label);
            RoleText = string.Join(" / ", roleTexts.Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        if (string.IsNullOrWhiteSpace(RoleText))
        {
            RoleText = "参演";
        }
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
    private async Task ToggleFollowAsync()
    {
        if (User is null || string.IsNullOrEmpty(User.Id))
        {
            return;
        }

        if (!_apiClient.GetAuthenticationState().IsAuthenticated)
        {
            _appViewModel.ShowTipCommand.Execute(("请先登录后再关注", InfoType.Warning));
            return;
        }

        IsLoading = true;
        try
        {
            var next = !IsFollowed;
            var res = await _apiClient.SetFollowAsync(User.Id, next);
            if (res.Succeeded)
            {
                IsFollowed = next;
                _appViewModel.ShowTipCommand.Execute(
                    (next ? "关注成功" : "已取消关注", InfoType.Success)
                );
            }
            else
            {
                _appViewModel.ShowTipCommand.Execute((res.Message ?? "操作失败", InfoType.Error));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关注用户操作异常.");
            _appViewModel.ShowTipCommand.Execute(("关注操作失败", InfoType.Error));
        }
        finally
        {
            IsLoading = false;
        }
    }
}
