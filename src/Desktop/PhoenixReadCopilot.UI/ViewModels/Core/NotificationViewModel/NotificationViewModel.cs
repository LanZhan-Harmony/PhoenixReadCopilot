using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.UI.Models.Settings;
using PhoenixReadCopilot.UI.Services;
using Richasy.BiliKernel.Bili.Moment;
using Windows.ApplicationModel;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.UI.ViewModels.Core;

/// <summary>
/// 通知视图模型.
/// </summary>
public sealed partial class NotificationViewModel : ViewModelBase
{
    private readonly AppViewModel _appViewModel;
    private readonly AppSettings Settings;

    public NotificationViewModel(
        IMomentDiscoveryService momentService,
        AppViewModel appViewModel,
        ISettingsService settingsService,
        ILogger<NotificationViewModel> logger
    )
    {
        _momentService = momentService;
        _appViewModel = appViewModel;
        Settings = settingsService.Settings;
        _logger = logger;
    }

    [RelayCommand]
    private async Task TryStartAsync()
    {
        var isNotifyEnabled = Settings.IsNotificationEnabled;
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

        await InitializeCommand.ExecuteAsync(default);
        TrySendVideoMomentNotificationsCommand.Execute(default);
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (PhoenixReadCopilot.UI.Helpers.RuntimeHelper.IsMSIX)
        {
            try
            {
                var entry = (await Package.Current.GetAppListEntriesAsync()).FirstOrDefault();
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
        TrySendVideoMomentNotificationsCommand.Execute(default);
}
