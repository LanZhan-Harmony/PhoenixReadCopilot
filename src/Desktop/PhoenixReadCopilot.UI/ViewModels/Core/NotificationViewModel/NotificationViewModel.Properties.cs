using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.UI.Models.Settings;
using Richasy.BiliKernel.Bili.Moment;

namespace PhoenixReadCopilot.UI.ViewModels.Core;

/// <summary>
/// 通知视图模型.
/// </summary>
public sealed partial class NotificationViewModel
{
    private readonly ILogger<NotificationViewModel> _logger;
    private readonly IMomentDiscoveryService _momentService;
    private readonly AppViewModel _appViewModel;
    private readonly AppSettings Settings;

    private DispatcherTimer _timer;
    private bool _isTileSupport;

    private List<string> _momentIds;
}
