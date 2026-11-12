using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Models.User;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.ViewModels.Components;

/// <summary>
/// 账户视图模型.
/// </summary>
public sealed partial class AccountViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly NavigationViewModel _navService;
    private readonly ILogger<AccountViewModel> _logger;
    private readonly AppViewModel _appViewModel;

    [ObservableProperty]
    public partial UserDetailProfile? MyProfile { get; set; }

    [ObservableProperty]
    public partial bool IsInitializing { get; set; }

    [ObservableProperty]
    public partial int MomentCount { get; set; }

    [ObservableProperty]
    public partial int FollowCount { get; set; }

    [ObservableProperty]
    public partial int FansCount { get; set; }

    [ObservableProperty]
    public partial string Introduce { get; set; } = string.Empty;
}
