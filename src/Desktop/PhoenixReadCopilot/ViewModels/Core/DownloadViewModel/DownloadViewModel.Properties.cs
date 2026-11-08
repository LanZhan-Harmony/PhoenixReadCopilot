using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Models.Settings;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 下载视图模型.
/// </summary>
public sealed partial class DownloadViewModel
{
    private readonly ILogger<DownloadViewModel> _logger;
    private readonly AppViewModel _appViewModel;
    private readonly AppSettings _settings;
    private readonly string _ffmpegPath;
    private readonly string _downloadPath;
    private readonly bool _openFolderAfterDownload;
    private int _currentPartIndex;
    private readonly PhoenixReadApiClient _apiClient;
    private readonly HttpClient _httpClient;

    private string _seriesId = string.Empty;
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
