using PhoenixReadApi;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 认证与账号服务接口.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// 是否已登录.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 当前登录用户信息（未登录时为 null）.
    /// </summary>
    AuthenticatedUser? CurrentUser { get; }

    /// <summary>
    /// 登录状态发生变化时触发（参数为当前是否已登录）.
    /// </summary>
    event EventHandler<bool>? StateChanged;

    /// <summary>
    /// 初始化并验证登录会话.
    /// </summary>
    Task<bool> InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取支持的国家/地区区号列表.
    /// </summary>
    Task<IReadOnlyList<LoginRegion>> GetLoginRegionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送手机短信验证码.
    /// </summary>
    Task<VerificationCodeResult> SendVerificationCodeAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 使用手机短信验证码登录.
    /// </summary>
    Task<AuthenticatedUser> LoginWithSmsAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 创建网页扫码登录会话.
    /// </summary>
    Task<QrCodeLoginSession> CreateQrCodeLoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查网页扫码登录确认状态.
    /// </summary>
    Task<QrCodeCheckResult> CheckQrCodeStatusAsync(
        string token,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 刷新当前用户信息.
    /// </summary>
    Task<AuthenticatedUser?> RefreshUserInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 退出登录.
    /// </summary>
    Task LogoutAsync();
}
