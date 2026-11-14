using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Models.Player;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 下载视图模型 (负责分集/清晰度选择及命令转发至 IDownloadService).
/// </summary>
public sealed partial class DownloadViewModel : ViewModelBase
{
    public DownloadViewModel(
        ILogger<DownloadViewModel> logger,
        IDownloadService downloadService,
        INotificationService notificationService
    )
    {
        _logger = logger;
        _downloadService = downloadService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// 初始化基础数据.
    /// </summary>
    public void InitializeMetas(
        IReadOnlyCollection<PlayerFormatInformation>? formats,
        int currentPart = 1
    )
    {
        _currentPartIndex = currentPart;
        Formats = formats is null ? [] : [.. formats];
        MetaInitialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 初始化红果短剧元数据.
    /// </summary>
    public void InitializeDrama(
        string dramaId,
        string title,
        string coverUrl,
        IReadOnlyList<Episode>? episodes = null,
        Episode? currentEpisode = null
    )
    {
        _dramaId = dramaId;
        _seriesTitle = title;
        _coverUrl = coverUrl;
        Episodes = episodes is not null ? [.. episodes] : null;
        CurrentEpisode = currentEpisode;
    }

    /// <summary>
    /// 清除数据.
    /// </summary>
    public void Clear()
    {
        _dramaId = string.Empty;
        _seriesTitle = string.Empty;
        _coverUrl = string.Empty;
        _currentPartIndex = 0;
        Episodes = null;
        CurrentEpisode = null;
        Formats = null;
    }

    /// <summary>
    /// 下载当前分集视频.
    /// </summary>
    [RelayCommand]
    private async Task DownloadWithFormatAsync(PlayerFormatInformation? format = null)
    {
        Episode? ep = CurrentEpisode
            ?? Episodes?.FirstOrDefault(e => e.Number == _currentPartIndex)
            ?? Episodes?.FirstOrDefault();
        if (ep is null)
        {
            await _notificationService.ShowInAppNotificationAsync(
                "未能获取当前分集信息",
                NotificationType.Warning
            );
            return;
        }

        string epTitle = string.IsNullOrWhiteSpace(ep.Title) ? $"第{ep.Number}集" : ep.Title;
        await _downloadService.DownloadSingleEpisodeAsync(
            _dramaId,
            ep.VideoId,
            ep.Number,
            epTitle,
            _seriesTitle,
            format
        );
    }

    /// <summary>
    /// 下载封面.
    /// </summary>
    [RelayCommand]
    private async Task DownloadCoverAsync()
    {
        await _downloadService.DownloadCoverAsync(_coverUrl, _seriesTitle);
    }

    /// <summary>
    /// 下载弹幕.
    /// </summary>
    [RelayCommand]
    private async Task DownloadDanmakuAsync()
    {
        Episode? ep = CurrentEpisode
            ?? Episodes?.FirstOrDefault(e => e.Number == _currentPartIndex)
            ?? Episodes?.FirstOrDefault();
        if (ep is null || string.IsNullOrWhiteSpace(ep.VideoId))
        {
            await _notificationService.ShowInAppNotificationAsync(
                "无可用弹幕数据",
                NotificationType.Warning
            );
            return;
        }

        await _downloadService.DownloadDanmakuAsync(_dramaId, ep.VideoId, ep.Number, _seriesTitle);
    }

    /// <summary>
    /// 批量下载所有分集.
    /// </summary>
    [RelayCommand]
    private async Task BatchDownloadAllPartsAsync()
    {
        if (string.IsNullOrWhiteSpace(_dramaId) || Episodes is not { Count: > 0 })
        {
            await _notificationService.ShowInAppNotificationAsync(
                "无分集可下载",
                NotificationType.Warning
            );
            return;
        }

        _ = Task.Run(async () =>
        {
            await _downloadService.BatchDownloadEpisodesAsync(_dramaId, _seriesTitle, Episodes);
        });
    }

    /// <summary>
    /// 选择分集下载.
    /// </summary>
    [RelayCommand]
    private async Task BatchDownloadSelectedPartsAsync(string selection)
    {
        if (string.IsNullOrWhiteSpace(_dramaId) || Episodes is not { Count: > 0 })
        {
            return;
        }

        List<int> indices = ParseSelection(selection, Episodes.Count);
        if (indices.Count == 0)
        {
            await _notificationService.ShowInAppNotificationAsync(
                "分集选择格式不正确，例如: 1-5 或 1,3,5",
                NotificationType.Warning
            );
            return;
        }

        List<Episode> selectedEpisodes = Episodes.Where(e => indices.Contains(e.Number)).ToList();

        if (selectedEpisodes.Count == 0)
        {
            await _notificationService.ShowInAppNotificationAsync(
                "未匹配到指定的分集序号",
                NotificationType.Warning
            );
            return;
        }

        _ = Task.Run(async () =>
        {
            await _downloadService.BatchDownloadEpisodesAsync(
                _dramaId,
                _seriesTitle,
                selectedEpisodes
            );
        });
    }

    private static List<int> ParseSelection(string selection, int max)
    {
        HashSet<int> result = [];
        string[] parts = selection.Split([',', ' ', '，'], StringSplitOptions.RemoveEmptyEntries);
        foreach (string p in parts)
        {
            if (p.Contains('-'))
            {
                string[] range = p.Split('-');
                if (
                    range.Length == 2
                    && int.TryParse(range[0], out int start)
                    && int.TryParse(range[1], out int end)
                )
                {
                    for (int i = Math.Max(1, start); i <= Math.Min(max, end); i++)
                    {
                        result.Add(i);
                    }
                }
            }
            else if (int.TryParse(p, out int num) && num >= 1 && num <= max)
            {
                result.Add(num);
            }
        }
        return [.. result.OrderBy(i => i)];
    }

    /// <summary>
    /// 下载字幕.
    /// </summary>
    [RelayCommand]
    private void DownloadSubtitle() { }
}
