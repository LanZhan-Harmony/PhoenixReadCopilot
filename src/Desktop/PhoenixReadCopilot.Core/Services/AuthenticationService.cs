using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 认证与账号服务实现.
/// </summary>
public sealed class AuthenticationService(
    PhoenixReadApiClient apiClient,
    ILogger<AuthenticationService> logger
) : IAuthenticationService
{
    private readonly PhoenixReadApiClient _apiClient = apiClient;
    private readonly ILogger<AuthenticationService> _logger = logger;

    /// <inheritdoc/>
    public bool IsAuthenticated { get; private set; }

    /// <inheritdoc/>
    public AuthenticatedUser? CurrentUser { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<bool>? StateChanged;

    /// <inheritdoc/>
    public async Task<bool> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            bool isValid = await _apiClient.ValidateSessionAsync(cancellationToken);
            if (isValid)
            {
                CurrentUser = await _apiClient.GetCurrentUserInfoAsync(cancellationToken);
                IsAuthenticated = true;
            }
            else
            {
                CurrentUser = null;
                IsAuthenticated = false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"验证登录会话失败，进入未登录状态.");
            CurrentUser = null;
            IsAuthenticated = false;
        }

        StateChanged?.Invoke(this, IsAuthenticated);
        return IsAuthenticated;
    }

    /// <inheritdoc/>
    public IReadOnlyList<LoginRegion> GetLoginRegions() => _apiClient.GetLoginRegions();

    /// <inheritdoc/>
    public Task<VerificationCodeResult> SendVerificationCodeAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default
    ) => _apiClient.SendVerificationCodeAsync(phoneNumber, cancellationToken);

    /// <inheritdoc/>
    public async Task<AuthenticatedUser> LoginWithSmsAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default
    )
    {
        AuthenticatedUser user = await _apiClient.LoginAsync(phoneNumber, code, cancellationToken);
        CurrentUser = user;
        IsAuthenticated = true;
        StateChanged?.Invoke(this, true);
        return user;
    }

    /// <inheritdoc/>
    public Task<QrCodeLoginSession> CreateQrCodeLoginAsync(
        CancellationToken cancellationToken = default
    ) => _apiClient.CreateQrCodeLoginAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<QrCodeCheckResult> CheckQrCodeStatusAsync(
        string token,
        CancellationToken cancellationToken = default
    )
    {
        QrCodeCheckResult result = await _apiClient.CheckQrCodeStatusAsync(
            token,
            cancellationToken
        );
        if (result.Status == QrCodeLoginStatus.Confirmed)
        {
            CurrentUser = result.User
                ?? await _apiClient.GetCurrentUserInfoAsync(cancellationToken);
            IsAuthenticated = true;
            StateChanged?.Invoke(this, true);
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<AuthenticatedUser?> RefreshUserInfoAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (!IsAuthenticated)
        {
            return null;
        }

        try
        {
            CurrentUser = await _apiClient.GetCurrentUserInfoAsync(cancellationToken);
            return CurrentUser;
        }
        catch (Exception ex)
        {
            _logger.ZLogWarning(ex, $"刷新用户信息失败.");
            return CurrentUser;
        }
    }

    /// <inheritdoc/>
    public Task LogoutAsync()
    {
        _apiClient.Logout();
        CurrentUser = null;
        IsAuthenticated = false;
        StateChanged?.Invoke(this, false);
        return Task.CompletedTask;
    }
}
