using CommunityToolkit.Mvvm.Input;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 通知视图模型.
/// </summary>
public sealed partial class NotificationViewModel : INotificationViewModel
{
    [RelayCommand]
    private async Task ShowTipAsync((string, InfoType) data) =>
        await _appViewModel.ShowTipCommand.ExecuteAsync(data);
}
