using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Models;
using PhoenixReadCopilot.Services;
using PhoenixReadCopilot.ViewModels.Items;
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
        string seriesId,
        string title,
        string coverUrl,
        IReadOnlyList<Episode>? episodes = default,
        Episode? currentEpisode = default
    )
    {
        _seriesId = seriesId;
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
        _seriesId = string.Empty;
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
    private async Task DownloadWithFormatAsync(PlayerFormatInformation? format = default)
    {
        if (CurrentEpisode is not null)
        {
            var ep = CurrentEpisode;
            await DownloadSingleEpisodeAsync(
                _seriesId,
                ep.VideoId,
                ep.Number,
                string.IsNullOrEmpty(ep.Title) ? $"第{ep.Number}集" : ep.Title,
                format
            );
            return;
        }

        if (Episodes is { Count: > 0 })
        {
            var ep = Episodes.FirstOrDefault(e => e.Number == _currentPartIndex) ?? Episodes[0];
            await DownloadSingleEpisodeAsync(
                _seriesId,
                ep.VideoId,
                ep.Number,
                string.IsNullOrEmpty(ep.Title) ? $"第{ep.Number}集" : ep.Title,
                format
            );
            return;
        }

        _appViewModel.ShowTipCommand.Execute(("未能获取当前分集信息", InfoType.Warning));
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
        string? videoId = CurrentEpisode?.VideoId;
        int epNumber = CurrentEpisode?.Number ?? _currentPartIndex;

        if (string.IsNullOrEmpty(videoId) && Episodes is { Count: > 0 })
        {
            var ep = Episodes.FirstOrDefault(e => e.Number == _currentPartIndex) ?? Episodes[0];
            videoId = ep.VideoId;
            epNumber = ep.Number;
        }

        if (string.IsNullOrEmpty(videoId) || string.IsNullOrEmpty(_seriesId))
        {
            _appViewModel.ShowTipCommand.Execute(("无可用弹幕数据", InfoType.Warning));
            return;
        }

        try
        {
            var danmakuPage = await _apiClient.GetDanmakuAsync(_seriesId, videoId, 0, 3600000);
            var fileName = $"{SanitizeFileName(_seriesTitle)}_第{epNumber}集_弹幕.xml";
            var savePath = Path.Combine(_downloadPath, fileName);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<i>");
            sb.AppendLine("<chatserver>phoenixread</chatserver>");
            sb.AppendLine($"<chatid>{videoId}</chatid>");
            sb.AppendLine("<mission>0</mission>");
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
        if (string.IsNullOrEmpty(_seriesId) || Episodes is not { Count: > 0 })
        {
            _appViewModel.ShowTipCommand.Execute(("无分集可下载", InfoType.Warning));
            return;
        }

        var episodesToDownload = Episodes.ToList();
        _appViewModel.ShowTipCommand.Execute(
            ($"开始后台批量下载全部 {episodesToDownload.Count} 集...", InfoType.Information)
        );
        _ = Task.Run(async () =>
        {
            foreach (var ep in episodesToDownload)
            {
                await DownloadSingleEpisodeAsync(
                    _seriesId,
                    ep.VideoId,
                    ep.Number,
                    string.IsNullOrEmpty(ep.Title) ? $"第{ep.Number}集" : ep.Title,
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
        if (string.IsNullOrEmpty(_seriesId) || Episodes is not { Count: > 0 })
        {
            return;
        }

        var totalCount = Episodes.Count;
        var indices = ParseSelection(selection, totalCount);
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
                var ep = Episodes.FirstOrDefault(e => e.Number == idx);
                if (ep is not null)
                {
                    await DownloadSingleEpisodeAsync(
                        _seriesId,
                        ep.VideoId,
                        ep.Number,
                        string.IsNullOrEmpty(ep.Title) ? $"第{ep.Number}集" : ep.Title,
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
        PlayerFormatInformation? preferFormat = default,
        bool silent = false
    )
    {
        try
        {
            if (!silent)
            {
                _appViewModel.ShowTipCommand.Execute(
                    ($"正在解析第 {index} 集直链地址...", InfoType.Information)
                );
            }

            var mediaList = await _apiClient.ResolveMediaAsync(seriesId, videoId);
            if (mediaList is null || mediaList.Count == 0)
            {
                if (!silent)
                {
                    _appViewModel.ShowTipCommand.Execute(
                        ($"第 {index} 集视频地址解析失败", InfoType.Error)
                    );
                }
                return;
            }

            // 如果指定了画质，优先匹配指定清晰度；否则取最高画质
            PlaybackMedia? media = null;
            if (preferFormat is not null)
            {
                media = mediaList.FirstOrDefault(m =>
                    PlayerFormatItemViewModel.GetQualityScore(m) == preferFormat.Quality
                );
            }
            media ??= mediaList
                .OrderByDescending(PlayerFormatItemViewModel.GetQualityScore)
                .FirstOrDefault();

            if (media is null || string.IsNullOrEmpty(media.Url))
            {
                if (!silent)
                {
                    _appViewModel.ShowTipCommand.Execute(
                        ($"第 {index} 集视频直链不存在", InfoType.Error)
                    );
                }
                return;
            }

            var fileName = $"{SanitizeFileName(_seriesTitle)} - 第{index}集.mp4";
            var outputPath = Path.Combine(_downloadPath, fileName);
            var tempPath = Path.Combine(_downloadPath, $".temp_{index}_{Guid.NewGuid():N}.mp4");

            try
            {
                if (!silent)
                {
                    _appViewModel.ShowTipCommand.Execute(
                        ($"正在高速下载第 {index} 集数据...", InfoType.Information)
                    );
                }

                // 统一通过 HttpClient 下载媒体原始数据流，避免 FFmpeg 访问网络因 UA/握手被 CDN 返回 403 / EIO -5
                using var request = new HttpRequestMessage(HttpMethod.Get, media.Url);
                var referer = !string.IsNullOrEmpty(media.Referer) ? media.Referer : "https://novel.snssdk.com";
                request.Headers.Referrer = new Uri(referer);
                request.Headers.TryAddWithoutValidation(
                    "User-Agent",
                    "com.phoenix.read/73532 (Linux; U; Android 16; zh_CN; 25053RT47C; Build/BP2A.250605.031.A3; Cronet/TTNetVersion:04657795 2026-01-23 QuicVersion:c67e9834 2025-09-08)"
                );

                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    await using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs);
                }

                // 1. 无 CENC 加密，直接将临时文件移动为正式输出文件
                if (string.IsNullOrEmpty(media.DecryptionKey))
                {
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                    File.Move(tempPath, outputPath);

                    if (!silent)
                    {
                        _appViewModel.ShowTipCommand.Execute(
                            ($"第 {index} 集已成功下载至: {outputPath}", InfoType.Success)
                        );
                        if (_openFolderAfterDownload)
                        {
                            Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
                        }
                    }
                    return;
                }

                // 2. 包含 CENC AES-128 加密密钥，调用本地 FFmpeg 对本地文件进行极速解密解包 (无网络依赖，毫秒级完成)
                var ffmpegExe = ResolveFFmpegPath();
                if (string.IsNullOrEmpty(ffmpegExe))
                {
                    _appViewModel.ShowTipCommand.Execute(
                        (
                            $"第 {index} 集数据已下载，但包含 CENC 加密，未在系统环境变量或应用目录找到 ffmpeg.exe，无法自动解密",
                            InfoType.Error
                        )
                    );
                    return;
                }

                if (!silent)
                {
                    _appViewModel.ShowTipCommand.Execute(
                        ($"正在通过 FFmpeg 本地解密第 {index} 集...", InfoType.Information)
                    );
                }

                var args = new System.Text.StringBuilder();
                args.Append("-y ");
                args.Append($"-decryption_key {media.DecryptionKey} ");
                args.Append($"-i \"{tempPath}\" -c copy \"{outputPath}\"");

                var startInfo = new ProcessStartInfo(ffmpegExe, args.ToString())
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var errorOutput = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        if (File.Exists(tempPath))
                        {
                            try { File.Delete(tempPath); } catch { }
                        }

                        if (!silent)
                        {
                            _appViewModel.ShowTipCommand.Execute(
                                ($"第 {index} 集已成功解密下载至: {outputPath}", InfoType.Success)
                            );
                            if (_openFolderAfterDownload)
                            {
                                Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError($"FFmpeg 本地解密第 {index} 集失败 (exit code {process.ExitCode}): {errorOutput}");
                        if (!silent)
                        {
                            _appViewModel.ShowTipCommand.Execute(
                                (
                                    $"第 {index} 集解密失败 (FFmpeg 退出代码: {process.ExitCode})",
                                    InfoType.Error
                                )
                            );
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(tempPath) && File.Exists(outputPath))
                {
                    try { File.Delete(tempPath); } catch { }
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

    private string? ResolveFFmpegPath()
    {
        if (File.Exists(_ffmpegPath))
        {
            return _ffmpegPath;
        }

        var appBaseFfmpeg = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(appBaseFfmpeg))
        {
            return appBaseFfmpeg;
        }

        // 探测系统 PATH 中是否存在 ffmpeg
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in paths)
        {
            try
            {
                var candidate = Path.Combine(p, "ffmpeg.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch { }
        }

        return null;
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
    /// 下载字幕.
    /// </summary>
    [RelayCommand]
    private void DownloadSubtitle() { }
}
