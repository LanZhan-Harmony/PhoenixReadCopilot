using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Player;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 下载视图模型.
/// </summary>
public sealed partial class DownloadViewModel
{
    private readonly ILogger<DownloadViewModel> _logger;
    private readonly IDownloadService _downloadService;
    private readonly INotificationService _notificationService;

    private int _currentPartIndex;
    private string _dramaId = string.Empty;
    private string _seriesTitle = string.Empty;
    private string _coverUrl = string.Empty;

    [ObservableProperty]
    public partial List<PlayerFormatInformation>? Formats { get; set; }

    [ObservableProperty]
    public partial List<Episode>? Episodes { get; set; }

    [ObservableProperty]
    public partial Episode? CurrentEpisode { get; set; }

    [ObservableProperty]
    public partial bool HasAvailableSubtitle { get; set; }

    /// <summary>
    /// 元数据初始化完成事件.
    /// </summary>
    public event EventHandler? MetaInitialized;
}
