using CommunityToolkit.Mvvm.Input;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 通知视图模型.
/// </summary>
public sealed partial class NotificationViewModel
{
    [RelayCommand]
    private static Task TrySendVideoMomentNotificationsAsync()
    {
        // TODO: 红果短剧动态通知待接入
        return Task.CompletedTask;
    }
}
