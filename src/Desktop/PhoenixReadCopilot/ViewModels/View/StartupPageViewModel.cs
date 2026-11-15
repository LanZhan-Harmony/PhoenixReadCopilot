using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PhoenixReadApi;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.ViewModels;
using ZLogger;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 启动页视图模型.
/// </summary>
public sealed partial class StartupPageViewModel : ViewModelBase
{
    private DispatcherTimer? _countdownTimer;
    private readonly PhoenixReadApiClient _apiClient;
    private readonly ILogger<StartupPageViewModel> _logger;
    private readonly DispatcherQueue _dispatcherQueue;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupPageViewModel"/> class.
    /// </summary>
    public StartupPageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<StartupPageViewModel> logger,
        DispatcherQueue dispatcherQueue,
        AppViewModel appViewModel
    )
    {
        _apiClient = apiClient;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
        _appViewModel = appViewModel;
    }

    /// <summary>
    /// 初始化视图模型.
    /// </summary>
    public async Task InitializeAsync(Image qrcodeImageControl)
    {
        Version = RuntimeHelper.AppVersion;
        QRCodeImage = qrcodeImageControl;
        await ReloadQRCodeAsync();
    }

    public async Task ReloadQRCodeAsync()
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken ct = _cancellationTokenSource.Token;

        try
        {
            IsQRCodeLoading = true;
            ErrorTip = string.Empty;

            QrCodeLoginSession session = await _apiClient.CreateQrCodeLoginAsync(ct);
            if (session.QrCodeImageBytes is { Length: > 0 })
            {
                RenderQRCode(session.QrCodeImageBytes);
            }
            else
            {
                IsQRCodeLoading = false;
            }

            // 轮询检查扫码登录状态
            _ = Task.Run(
                async () =>
                {
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            await Task.Delay(2000, ct);
                            if (ct.IsCancellationRequested)
                            {
                                break;
                            }

                            QrCodeCheckResult result = await _apiClient.CheckQrCodeStatusAsync(
                                session.Token,
                                ct
                            );
                            if (result.Status == QrCodeLoginStatus.Confirmed)
                            {
                                _dispatcherQueue.TryEnqueue(() =>
                                {
                                    RequestClose?.Invoke(this, EventArgs.Empty);
                                    if (_appViewModel.Windows.Any(p => p is StartupWindow))
                                    {
                                        _appViewModel.EnterMainWindow();
                                    }
                                    else
                                    {
                                        _ = ItemServices.Account.InitializeAsync();
                                    }
                                });
                                break;
                            }
                            else if (result.Status == QrCodeLoginStatus.Expired)
                            {
                                _dispatcherQueue.TryEnqueue(() =>
                                {
                                    ErrorTip = "二维码已过期，请刷新";
                                });
                                break;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.ZLogWarning(ex, $"检查二维码状态异常");
                        }
                    }
                },
                ct
            );
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            IsQRCodeLoading = false;
            ErrorTip = ex.Message;
            _logger.ZLogError(ex, $"获取扫码二维码失败.");
        }
    }

    [RelayCommand]
    private async Task SendVerificationCodeAsync()
    {
        string phone = PhoneNumber.Trim();
        if (string.IsNullOrWhiteSpace(phone) || phone.Length != 11)
        {
            ErrorTip = "请输入正确的11位手机号码";
            return;
        }

        try
        {
            IsSmsSending = true;
            ErrorTip = string.Empty;
            VerificationCodeResult result = await _apiClient.SendVerificationCodeAsync(phone);
            int wait = result.RetryAfterSeconds > 0 ? result.RetryAfterSeconds : 60;
            StartCountdown(wait);
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"发送短信验证码失败");
            ErrorTip = ex.Message;
        }
        finally
        {
            IsSmsSending = false;
        }
    }

    private void StartCountdown(int seconds)
    {
        _countdownTimer?.Stop();
        SendCodeCountdown = seconds;
        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += (s, e) =>
        {
            SendCodeCountdown--;
            if (SendCodeCountdown <= 0)
            {
                _countdownTimer.Stop();
            }
        };
        _countdownTimer.Start();
    }

    [RelayCommand]
    private async Task LoginWithSmsAsync()
    {
        string phone = PhoneNumber.Trim();
        string code = VerificationCode.Trim();

        if (string.IsNullOrWhiteSpace(phone) || phone.Length != 11)
        {
            ErrorTip = "请输入正确的11位手机号码";
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            ErrorTip = "请输入收到的短信验证码";
            return;
        }

        try
        {
            IsLoggingIn = true;
            ErrorTip = string.Empty;
            await _apiClient.LoginAsync(phone, code);
            _dispatcherQueue.TryEnqueue(() =>
            {
                RequestClose?.Invoke(this, EventArgs.Empty);
                if (_appViewModel.Windows.Any(p => p is StartupWindow))
                {
                    _appViewModel.EnterMainWindow();
                }
                else
                {
                    _ = ItemServices.Account.InitializeAsync();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"短信登录失败");
            ErrorTip = ex.Message;
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    [RelayCommand]
    private void SkipLogin()
    {
        if (_cancellationTokenSource is not null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        _countdownTimer?.Stop();
        RequestClose?.Invoke(this, EventArgs.Empty);
        if (_appViewModel.Windows.Any(p => p is StartupWindow))
        {
            _appViewModel.EnterMainWindow();
        }
    }

    public void Exit(bool shouldRestart) => _ = ExitAsync(shouldRestart);

    public async Task ExitAsync(bool shouldRestart)
    {
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        _countdownTimer?.Stop();

        if (shouldRestart)
        {
            AppViewModel.Restart();
        }
    }

    public void RenderQRCode(byte[] imageData)
    {
        if (QRCodeImage is null)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(async () =>
        {
            using MemoryStream stream = new(imageData);
            BitmapImage bitmap = new();
            await bitmap.SetSourceAsync(stream.AsRandomAccessStream()).AsTask();
            QRCodeImage.Source = bitmap;
            IsQRCodeLoading = false;
        });
    }
}
