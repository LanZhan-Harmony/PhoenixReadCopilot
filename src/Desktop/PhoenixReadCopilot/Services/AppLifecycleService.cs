using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.BadgeNotifications;
using PhoenixReadCopilot.Contracts.Services;
using PhoenixReadCopilot.Helpers;

namespace PhoenixReadCopilot.Services;

/// <summary>
/// 应用程序生命周期与全局状态服务实现.
/// </summary>
internal sealed class AppLifecycleService : IAppLifecycleService
{
    private bool _isInitialLoading;

    public bool IsInitialLoading
    {
        get => _isInitialLoading;
        set
        {
            if (_isInitialLoading != value)
            {
                _isInitialLoading = value;
                InitialLoadingChanged?.Invoke(this, value);
            }
        }
    }

    public event EventHandler<bool>? InitialLoadingChanged;

    public AppLifecycleService()
    {
    }

    public void Launch()
    {
        ClearBadge();
        IsInitialLoading = true;
    }

    public void ClearBadge()
    {
        // 清除所有之前的badges（仅在 MSIX 打包环境下支持）.
        if (RuntimeHelper.IsMSIX)
        {
            try
            {
                BadgeNotificationManager.Current.ClearBadge();
            }
            catch { }
        }
    }

    public void Restart()
    {
        AppInstance.GetCurrent().UnregisterKey();
        AppInstance.Restart(null);
    }
}
