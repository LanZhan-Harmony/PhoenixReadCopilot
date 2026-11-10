using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PhoenixReadApi;
using PhoenixReadCopilot.Forms;
using PhoenixReadCopilot.ViewModels.Core;
using PhoenixReadCopilot.ViewModels.Items;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.View;

/// <summary>
/// 启动页视图模型.
/// </summary>
public sealed partial class StartupPageViewModel : ViewModelBase
{
    private DispatcherTimer? _countdownTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupPageViewModel"/> class.
    /// </summary>
    public StartupPageViewModel(
        PhoenixReadApiClient apiClient,
        ILogger<StartupPageViewModel> logger,
        DispatcherQueue dispatcherQueue,
        IAppToolkit appToolkit,
        AppViewModel appViewModel
    )
    {
        _apiClient = apiClient;
        _logger = logger;
        _dispatcherQueue = dispatcherQueue;
        _appToolkit = appToolkit;
        _appViewModel = appViewModel;
    }

    /// <summary>
    /// 初始化视图模型.
    /// </summary>
    [RelayCommand]
    private async Task InitializeAsync(Image qrcodeImageControl)
    {
        Version = _appToolkit.GetPackageVersion();
        QRCodeImage = qrcodeImageControl;
        await ReloadQRCodeAsync();
    }

    [RelayCommand]
    private async Task ReloadQRCodeAsync()
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
            _ = Task.Run(async () =>
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

                        QrCodeCheckResult result = await _apiClient.CheckQrCodeStatusAsync(session.Token, ct);
                        if (result.Status == QrCodeLoginStatus.Confirmed)
                        {
                            _dispatcherQueue.TryEnqueue(() =>
                            {
                                RequestClose?.Invoke(this, EventArgs.Empty);
                                if (_appViewModel.Windows.Any(p => p is StartupWindow))
                                {
                                    _appViewModel.EnterMainWindowCommand.Execute(null);
                                }
                                else
                                {
                                    ItemServices.Account.InitializeCommand.Execute(null);
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
                        _logger.LogWarning(ex, "检查二维码状态异常");
                    }
                }
            }, ct);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            IsQRCodeLoading = false;
            ErrorTip = ex.Message;
            _logger.LogError(ex, "获取扫码二维码失败.");
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
            _logger.LogError(ex, "发送短信验证码失败");
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
        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
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
                    _appViewModel.EnterMainWindowCommand.Execute(null);
                }
                else
                {
                    ItemServices.Account.InitializeCommand.Execute(null);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "短信登录失败");
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
            _appViewModel.EnterMainWindowCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task ExitAsync(bool shouldRestart)
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
            _appViewModel.RestartCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void RenderQRCode(byte[] imageData)
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
