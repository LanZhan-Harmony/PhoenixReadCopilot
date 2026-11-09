using System.Diagnostics;
using System.Text;
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
        string localDownloadFolder = _settings.DownloadFolder;
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
        IReadOnlyList<Episode>? episodes = null,
        Episode? currentEpisode = null
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
    private async Task DownloadWithFormatAsync(PlayerFormatInformation? format = null)
    {
        if (CurrentEpisode is not null)
        {
            Episode ep = CurrentEpisode;
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
            Episode ep = Episodes.FirstOrDefault(e => e.Number == _currentPartIndex) ?? Episodes[0];
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
            string fileName = $"{SanitizeFileName(_seriesTitle)}_cover.jpg";
            string savePath = Path.Combine(_downloadPath, fileName);
            byte[] bytes = await _httpClient.GetByteArrayAsync(_coverUrl);
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
            Episode ep = Episodes.FirstOrDefault(e => e.Number == _currentPartIndex) ?? Episodes[0];
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
            DanmakuPage danmakuPage = await _apiClient.GetDanmakuAsync(
                _seriesId,
                videoId,
                0,
                3600000
            );
            string fileName = $"{SanitizeFileName(_seriesTitle)}_第{epNumber}集_弹幕.xml";
            string savePath = Path.Combine(_downloadPath, fileName);

            StringBuilder sb = new();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<i>");
            sb.AppendLine("<chatserver>phoenixread</chatserver>");
            sb.AppendLine($"<chatid>{videoId}</chatid>");
            sb.AppendLine("<mission>0</mission>");
            sb.AppendLine($"<maxlimit>{danmakuPage.Items.Count}</maxlimit>");

            foreach (DanmakuItem item in danmakuPage.Items)
            {
                double timeSec = item.TimeMs / 1000.0;
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

        List<Episode> episodesToDownload = Episodes.ToList();
        _appViewModel.ShowTipCommand.Execute(
            ($"开始后台批量下载全部 {episodesToDownload.Count} 集...", InfoType.Information)
        );
        _ = Task.Run(async () =>
        {
            foreach (Episode? ep in episodesToDownload)
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

        int totalCount = Episodes.Count;
        List<int> indices = ParseSelection(selection, totalCount);
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
            foreach (int idx in indices)
            {
                Episode? ep = Episodes.FirstOrDefault(e => e.Number == idx);
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
        PlayerFormatInformation? preferFormat = null,
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

            IReadOnlyList<PlaybackMedia>? mediaList = await _apiClient.ResolveMediaAsync(
                seriesId,
                videoId
            );
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

            string fileName = $"{SanitizeFileName(_seriesTitle)} - 第{index}集.mp4";
            string outputPath = Path.Combine(_downloadPath, fileName);
            string tempPath = Path.Combine(_downloadPath, $".temp_{index}_{Guid.NewGuid():N}.mp4");

            try
            {
                if (!silent)
                {
                    _appViewModel.ShowTipCommand.Execute(
                        ($"正在高速下载第 {index} 集数据...", InfoType.Information)
                    );
                }

                // 统一通过 HttpClient 下载媒体原始数据流，避免 FFmpeg 访问网络因 UA/握手被 CDN 返回 403 / EIO -5
                using HttpRequestMessage request = new(HttpMethod.Get, media.Url);
                string referer = !string.IsNullOrEmpty(media.Referer)
                    ? media.Referer
                    : "https://novel.snssdk.com";
                request.Headers.Referrer = new Uri(referer);
                request.Headers.TryAddWithoutValidation(
                    "User-Agent",
                    "com.phoenix.read/73532 (Linux; U; Android 16; zh_CN; 25053RT47C; Build/BP2A.250605.031.A3; Cronet/TTNetVersion:04657795 2026-01-23 QuicVersion:c67e9834 2025-09-08)"
                );

                using (
                    HttpResponseMessage response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead
                    )
                )
                {
                    response.EnsureSuccessStatusCode();
                    await using FileStream fs = new(
                        tempPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None
                    );
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
                string? ffmpegExe = ResolveFFmpegPath();
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

                StringBuilder args = new();
                args.Append("-y ");
                args.Append($"-decryption_key {media.DecryptionKey} ");
                args.Append($"-i \"{tempPath}\" -c copy \"{outputPath}\"");

                ProcessStartInfo startInfo = new(ffmpegExe, args.ToString())
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };

                using Process? process = Process.Start(startInfo);
                if (process != null)
                {
                    string errorOutput = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        if (File.Exists(tempPath))
                        {
                            try
                            {
                                File.Delete(tempPath);
                            }
                            catch { }
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
                        _logger.LogError(
                            $"FFmpeg 本地解密第 {index} 集失败 (exit code {process.ExitCode}): {errorOutput}"
                        );
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
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch { }
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

        string appBaseFfmpeg = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
        if (File.Exists(appBaseFfmpeg))
        {
            return appBaseFfmpeg;
        }

        // 探测系统 PATH 中是否存在 ffmpeg
        string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        string[] paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (string p in paths)
        {
            try
            {
                string candidate = Path.Combine(p, "ffmpeg.exe");
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

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "短剧";
        }
        string invalid =
            new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        Regex regex = new($"[{Regex.Escape(invalid)}]");
        return regex.Replace(name, "_");
    }

    /// <summary>
    /// 下载字幕.
    /// </summary>
    [RelayCommand]
    private void DownloadSubtitle() { }
}
