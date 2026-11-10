using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Core;
using WinUIKernel.Share.Toolkits;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 启动页视图模型.
/// </summary>
public sealed partial class StartupPageViewModel
{
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<StartupPageViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly IAppToolkit _appToolkit;
    private readonly AppViewModel _appViewModel;

    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    public partial string Version { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorTip { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsQRCodeLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendCode))]
    public partial string PhoneNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VerificationCode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendCode))]
    [NotifyPropertyChangedFor(nameof(SendCodeButtonText))]
    public partial bool IsSmsSending { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendCode))]
    [NotifyPropertyChangedFor(nameof(SendCodeButtonText))]
    public partial int SendCodeCountdown { get; set; }

    [ObservableProperty]
    public partial bool IsLoggingIn { get; set; }

    /// <summary>
    /// 是否可以发送验证码.
    /// </summary>
    public bool CanSendCode => !IsSmsSending && SendCodeCountdown <= 0 && !string.IsNullOrWhiteSpace(PhoneNumber);

    /// <summary>
    /// 发送验证码按钮文字.
    /// </summary>
    public string SendCodeButtonText => SendCodeCountdown > 0 ? $"{SendCodeCountdown}s" : "获取验证码";

    /// <summary>
    /// 二维码图片控件.
    /// </summary>
    public Image? QRCodeImage { get; private set; }

    /// <summary>
    /// 请求关闭视图或弹窗.
    /// </summary>
    public event EventHandler? RequestClose;
}
