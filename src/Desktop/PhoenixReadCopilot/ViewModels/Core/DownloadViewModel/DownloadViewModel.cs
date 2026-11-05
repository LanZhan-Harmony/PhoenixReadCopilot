using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Services;
using Richasy.BiliKernel.Models.Media;
using WinUIKernel.Share.Base;
using WinUIKernel.Share.ViewModels;

namespace PhoenixReadCopilot.ViewModels.Core;

/// <summary>
/// 下载视图模型 (红果短剧专有解密下载引擎).
/// </summary>
public sealed partial class DownloadViewModel : ViewModelBase
{
    public DownloadViewModel(
        ILogger<DownloadViewModel> logger,
        AppViewModel appViewModel,
        ISettingsService settingsService,
        PhoenixReadApiClient apiClient
    )
    {
        _logger = logger;
        _appViewModel = appViewModel;
        _settings = settingsService.Settings;
        _apiClient = apiClient;
        _httpClient = new HttpClient();
        _ffmpegPath = _appViewModel.FFmpegPath;
        var localDownloadFolder = _settings.DownloadFolder;
        _downloadPath =
            string.IsNullOrEmpty(localDownloadFolder) || !Directory.Exists(localDownloadFolder)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    "PhoenixRead Downloads"
                )
                : localDownloadFolder;
        if (!Directory.Exists(_downloadPath))
        {
            Directory.CreateDirectory(_downloadPath);
        }

        _openFolderAfterDownload = _settings.OpenFolderAfterDownload;
    }

    /// <summary>
    /// 初始化基础数据.
    /// </summary>
    public void InitializeMetas(
        string url,
        IReadOnlyCollection<PlayerFormatInformation>? formats,
        IReadOnlyCollection<VideoPart>? parts = default,
        int currentPart = 1
    )
    {
        _currentPartIndex = currentPart;
        Formats = formats is null ? [] : [.. formats];
        Parts = parts is null ? default : [.. parts];
        MetaInitialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 初始化红果短剧元数据.
    /// </summary>
    public void InitializeDrama(string seriesId, string title, string coverUrl)
    {
        _seriesId = seriesId;
        _seriesTitle = title;
        _coverUrl = coverUrl;
    }

    /// <summary>
    /// 清除数据.
    /// </summary>
    public void Clear()
    {
        _seriesId = string.Empty;
        _seriesTitle = string.Empty;
        _coverUrl = string.Empty;
        _currentPartIndex = 0;
        Formats = null;
        Parts = null;
        Episodes = null;
    }

    /// <summary>
    /// 下载当前分集视频.
    /// </summary>
    [RelayCommand]
    private async Task DownloadWithFormatAsync(PlayerFormatInformation? format = default)
    {
        var part =
            Parts?.FirstOrDefault(p => p.Index == _currentPartIndex) ?? Parts?.FirstOrDefault();
        if (part is null)
        {
            _appViewModel.ShowTipCommand.Execute(("未能获取当前分集信息", InfoType.Warning));
            return;
        }

        await DownloadSingleEpisodeAsync(
            _seriesId,
            part.Identifier.Id,
            part.Index,
            part.Identifier.Title
        );
    }

    /// <summary>
    /// 下载封面.
    /// </summary>
    [RelayCommand]
    private async Task DownloadCoverAsync()
    {
        if (string.IsNullOrEmpty(_coverUrl))
        {
            _appViewModel.ShowTipCommand.Execute(("未找到封面地址", InfoType.Warning));
            return;
        }

        try
        {
            var fileName = $"{SanitizeFileName(_seriesTitle)}_cover.jpg";
            var savePath = Path.Combine(_downloadPath, fileName);
            var bytes = await _httpClient.GetByteArrayAsync(_coverUrl);
            await File.WriteAllBytesAsync(savePath, bytes);
            _appViewModel.ShowTipCommand.Execute(($"封面已下载至: {savePath}", InfoType.Success));
            if (_openFolderAfterDownload)
            {
                Process.Start("explorer.exe", $"/select,\"{savePath}\"");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载封面失败.");
            _appViewModel.ShowTipCommand.Execute(("下载封面失败", InfoType.Error));
        }
    }

    /// <summary>
    /// 下载弹幕.
    /// </summary>
    [RelayCommand]
    private async Task DownloadDanmakuAsync()
    {
        var part =
            Parts?.FirstOrDefault(p => p.Index == _currentPartIndex) ?? Parts?.FirstOrDefault();
        if (part is null || string.IsNullOrEmpty(_seriesId))
        {
            _appViewModel.ShowTipCommand.Execute(("无可用弹幕数据", InfoType.Warning));
            return;
        }

        try
        {
            var danmakuPage = await _apiClient.GetDanmakuAsync(
                _seriesId,
                part.Identifier.Id,
                0,
                3600000
            );
            var fileName = $"{SanitizeFileName(_seriesTitle)}_第{part.Index}集_弹幕.xml";
            var savePath = Path.Combine(_downloadPath, fileName);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<i>");
            sb.AppendLine($"<chatserver>phoenixread</chatserver>");
            sb.AppendLine($"<chatid>{part.Identifier.Id}</chatid>");
            sb.AppendLine($"<mission>0</mission>");
            sb.AppendLine($"<maxlimit>{danmakuPage.Items.Count}</maxlimit>");

            foreach (var item in danmakuPage.Items)
            {
                var timeSec = item.TimeMs / 1000.0;
                sb.AppendLine(
                    $"<d p=\"{timeSec:F2},1,25,16777215,0,0,0,0\">{System.Security.SecurityElement.Escape(item.Text)}</d>"
                );
            }
            sb.AppendLine("</i>");

            await File.WriteAllTextAsync(savePath, sb.ToString(), System.Text.Encoding.UTF8);
            _appViewModel.ShowTipCommand.Execute(($"弹幕已保存至: {savePath}", InfoType.Success));
            if (_openFolderAfterDownload)
            {
                Process.Start("explorer.exe", $"/select,\"{savePath}\"");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载弹幕失败.");
            _appViewModel.ShowTipCommand.Execute(("下载弹幕失败", InfoType.Error));
        }
    }

    /// <summary>
    /// 批量下载所有分集.
    /// </summary>
    [RelayCommand]
    private async Task BatchDownloadAllPartsAsync()
    {
        if (Parts is null || Parts.Count == 0 || string.IsNullOrEmpty(_seriesId))
        {
            _appViewModel.ShowTipCommand.Execute(("无分集可下载", InfoType.Warning));
            return;
        }

        _appViewModel.ShowTipCommand.Execute(
            ($"开始后台批量下载全部 {Parts.Count} 集...", InfoType.Information)
        );
        _ = Task.Run(async () =>
        {
            foreach (var part in Parts)
            {
                await DownloadSingleEpisodeAsync(
                    _seriesId,
                    part.Identifier.Id,
                    part.Index,
                    part.Identifier.Title,
                    silent: true
                );
            }
            _appViewModel.ShowTipCommand.Execute(("批量下载完成！", InfoType.Success));
            if (_openFolderAfterDownload)
            {
                Process.Start("explorer.exe", _downloadPath);
            }
        });
    }

    /// <summary>
    /// 选择分集下载.
    /// </summary>
    [RelayCommand]
    private async Task BatchDownloadSelectedPartsAsync(string selection)
    {
        if (Parts is null || Parts.Count == 0 || string.IsNullOrEmpty(_seriesId))
        {
            return;
        }

        var indices = ParseSelection(selection, Parts.Count);
        if (indices.Count == 0)
        {
            _appViewModel.ShowTipCommand.Execute(
                ("分集选择格式不正确，例如: 1-5 或 1,3,5", InfoType.Warning)
            );
            return;
        }

        _appViewModel.ShowTipCommand.Execute(
            ($"开始下载所选 {indices.Count} 个分集...", InfoType.Information)
        );
        _ = Task.Run(async () =>
        {
            foreach (var idx in indices)
            {
                var part = Parts.FirstOrDefault(p => p.Index == idx);
                if (part is not null)
                {
                    await DownloadSingleEpisodeAsync(
                        _seriesId,
                        part.Identifier.Id,
                        part.Index,
                        part.Identifier.Title,
                        silent: true
                    );
                }
            }
            _appViewModel.ShowTipCommand.Execute(("所选分集下载完成！", InfoType.Success));
            if (_openFolderAfterDownload)
            {
                Process.Start("explorer.exe", _downloadPath);
            }
        });
    }

    private async Task DownloadSingleEpisodeAsync(
        string seriesId,
        string videoId,
        int index,
        string title,
        bool silent = false
    )
    {
        try
        {
            if (!silent)
            {
                _appViewModel.ShowTipCommand.Execute(
                    ($"正在解析并下载第 {index} 集...", InfoType.Information)
                );
            }

            var mediaList = await _apiClient.ResolveMediaAsync(seriesId, videoId);
            var media = mediaList?.FirstOrDefault();
            if (media is null || string.IsNullOrEmpty(media.Url))
            {
                if (!silent)
                {
                    _appViewModel.ShowTipCommand.Execute(
                        ($"第 {index} 集视频地址解析失败", InfoType.Error)
                    );
                }
                return;
            }

            var fileName = $"{SanitizeFileName(_seriesTitle)} - 第{index}集.mp4";
            var outputPath = Path.Combine(_downloadPath, fileName);

            // 检查 ffmpeg 工具路径
            var ffmpegExe = File.Exists(_ffmpegPath) ? _ffmpegPath : "ffmpeg";
            var referer = !string.IsNullOrEmpty(media.Referer)
                ? media.Referer
                : "https://novel.snssdk.com";

            // 组装 FFmpeg 命令 (利用 -decryption_key 解密流)
            var args = new System.Text.StringBuilder();
            args.Append("-y ");
            if (!string.IsNullOrEmpty(media.DecryptionKey))
            {
                args.Append($"-decryption_key {media.DecryptionKey} ");
            }
            args.Append($"-referer \"{referer}\" ");
            args.Append($"-i \"{media.Url}\" -c copy \"{outputPath}\"");

            var startInfo = new ProcessStartInfo(ffmpegExe, args.ToString())
            {
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                if (process.ExitCode == 0)
                {
                    if (!silent)
                    {
                        _appViewModel.ShowTipCommand.Execute(
                            ($"第 {index} 集已成功下载解密至: {outputPath}", InfoType.Success)
                        );
                        if (_openFolderAfterDownload)
                        {
                            Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"FFmpeg 下载第 {index} 集退出码非零: {process.ExitCode}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"下载第 {index} 集失败.");
            if (!silent)
            {
                _appViewModel.ShowTipCommand.Execute(
                    ($"下载第 {index} 集失败: {ex.Message}", InfoType.Error)
                );
            }
        }
    }

    private static List<int> ParseSelection(string selection, int max)
    {
        var result = new HashSet<int>();
        var parts = selection.Split([',', ' ', '，'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            if (p.Contains('-'))
            {
                var range = p.Split('-');
                if (
                    range.Length == 2
                    && int.TryParse(range[0], out var start)
                    && int.TryParse(range[1], out var end)
                )
                {
                    for (int i = Math.Max(1, start); i <= Math.Min(max, end); i++)
                    {
                        result.Add(i);
                    }
                }
            }
            else if (int.TryParse(p, out var num) && num >= 1 && num <= max)
            {
                result.Add(num);
            }
        }
        return [.. result.OrderBy(i => i)];
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "短剧";
        }
        var invalid =
            new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var regex = new Regex($"[{Regex.Escape(invalid)}]");
        return regex.Replace(name, "_");
    }

    /// <summary>
    /// 初始化基础数据 (PGC 兼容).
    /// </summary>
    public void InitializeMetas(
        string epUrl,
        string ssUrl,
        IReadOnlyCollection<PlayerFormatInformation>? formats,
        IReadOnlyCollection<EpisodeInformation>? episodes = default,
        int currentEpisode = 1
    )
    {
        _currentPartIndex = currentEpisode;
        Formats = formats is null ? [] : [.. formats];
        Episodes = episodes is null ? default : [.. episodes];
        MetaInitialized?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 下载字幕.
    /// </summary>
    [RelayCommand]
    private void DownloadSubtitle() { }
}
