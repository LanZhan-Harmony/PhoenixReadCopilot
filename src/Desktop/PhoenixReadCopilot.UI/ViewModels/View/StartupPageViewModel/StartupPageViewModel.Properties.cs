using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Richasy.BiliKernel.Bili.Authorization;

namespace PhoenixReadCopilot.UI.ViewModels.View;

/// <summary>
/// 启动页视图模型.
/// </summary>
public sealed partial class StartupPageViewModel
{
    private readonly ILogger<StartupPageViewModel> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly DispatcherQueue _dispatcherQueue;

    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    public partial string Version { get; set; }

    [ObservableProperty]
    public partial string ErrorTip { get; set; }

    [ObservableProperty]
    public partial bool IsQRCodeLoading { get; set; }

    /// <summary>
    /// 二维码图片控件.
    /// </summary>
    public Image? QRCodeImage { get; private set; }
}
