using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Helpers;
using PhoenixReadCopilot.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 通知视图模型.
/// </summary>
public sealed partial class NotificationViewModel : ViewModelBase
{
    public NotificationViewModel(
        AppViewModel appViewModel,
        ISettingsService settingsService,
        ILogger<NotificationViewModel> logger
    )
    {
        _appViewModel = appViewModel;
        Settings = settingsService.Settings;
        _logger = logger;
    }

    [RelayCommand]
    private async Task TryStartAsync()
    {
        bool isNotifyEnabled = Settings.IsNotificationEnabled;
        if (!isNotifyEnabled)
        {
            return;
        }

        if (_timer is null)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(15);
            _timer.Tick += OnTimerTick;
        }

        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }

        await InitializeCommand.ExecuteAsync(null);
        TrySendVideoMomentNotificationsCommand.Execute(null);
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (RuntimeHelper.IsMSIX)
        {
            try
            {
                AppListEntry? entry = (
                    await Package.Current.GetAppListEntriesAsync()
                ).FirstOrDefault();
                if (entry is not null)
                {
                    _isTileSupport = StartScreenManager.GetDefault().SupportsAppListEntry(entry);
                    if (_isTileSupport)
                    {
                        TileUpdateManager
                            .CreateTileUpdaterForApplication()
                            .EnableNotificationQueue(true);
                    }
                }
            }
            catch { }
        }
    }

    private void OnTimerTick(object? sender, object e) =>
        TrySendVideoMomentNotificationsCommand.Execute(null);
}
