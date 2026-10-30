using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadCopilot.UI.ViewModels.Core;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.User;

namespace PhoenixReadCopilot.UI.ViewModels.Components;

/// <summary>
/// 账户视图模型.
/// </summary>
public sealed partial class AccountViewModel
{
    private readonly IMyProfileService _myProfileService;
    private readonly IMessageService _messageService;
    private readonly NavigationViewModel _navService;
    private readonly ILogger<AccountViewModel> _logger;

    [ObservableProperty]
    public partial UserDetailProfile MyProfile { get; set; }

    [ObservableProperty]
    public partial bool IsInitializing { get; set; }

    [ObservableProperty]
    public partial int MomentCount { get; set; }

    [ObservableProperty]
    public partial int FollowCount { get; set; }

    [ObservableProperty]
    public partial int FansCount { get; set; }

    [ObservableProperty]
    public partial string Introduce { get; set; }
}
