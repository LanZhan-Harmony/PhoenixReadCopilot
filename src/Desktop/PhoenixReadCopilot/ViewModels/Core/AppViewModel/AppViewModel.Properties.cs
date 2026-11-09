using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using PhoenixReadCopilot.Models.Settings;
using Richasy.BiliKernel.Bili.Authorization;
using WinUIKernel.Share.Toolkits;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 应用视图模型.
/// </summary>
public sealed partial class AppViewModel : ViewModelBase
{
    private readonly ILogger<AppViewModel> _logger;
    private readonly IBiliTokenResolver _tokenResolver;
    private readonly IAppToolkit _appToolkit;
    private readonly AppSettings _settings;

    [ObservableProperty]
    public partial Window ActivatedWindow { get; set; }

    [ObservableProperty]
    public partial bool IsInitialLoading { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateShown { get; set; }

    /// <summary>
    /// 已创建的窗口列表.
    /// </summary>
    public List<Window> Windows { get; } = [];

    /// <summary>
    /// FFmpeg 路径.
    /// </summary>
    public string FFmpegPath { get; private set; }

    /// <summary>
    /// 是否已关闭.
    /// </summary>
    public bool IsClosed { get; set; }
}
