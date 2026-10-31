using CommunityToolkit.Mvvm.Input;
using WinUIKernel.Share.Base;

namespace WinUIKernel.Share.ViewModels;

/// <summary>
/// Notification view model.
/// </summary>
public interface INotificationViewModel
{
    /// <summary>
    /// Show tip command.
    /// </summary>
    public IAsyncRelayCommand<(string, InfoType)> ShowTipCommand { get; }
}
