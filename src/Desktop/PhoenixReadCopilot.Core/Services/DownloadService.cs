using System.Diagnostics;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PhoenixReadApi;
using PhoenixReadCopilot.Core.Contracts.Services;
using PhoenixReadCopilot.Core.Helpers;
using PhoenixReadCopilot.Core.Models.Player;
using ZLogger;

namespace PhoenixReadCopilot.Core.Services;

/// <summary>
/// 短剧与媒体下载服务实现 (基于纯 C# CENC 解密引擎，无外部 FFmpeg 进程依赖).
/// </summary>
public sealed class DownloadService : IDownloadService, IDisposable
{
    private readonly ILogger<DownloadService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly PhoenixReadApiClient _apiClient;
    private readonly HttpClient _httpClient;

    public DownloadService(
        ILogger<DownloadService> logger,
        ISettingsService settingsService,
        INotificationService notificationService,
        PhoenixReadApiClient apiClient,
        HttpClient? httpClient = null
    )
    {
        _logger = logger;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _apiClient = apiClient;
        _httpClient = httpClient ?? new HttpClient();
    }

    /// <inheritdoc/>
    public string DownloadFolderPath
    {
        get
        {
            string folder = _settingsService.Settings.DownloadFolder;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    "PhoenixRead Downloads"
                );
            }

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return folder;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> DownloadSingleEpisodeAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        string episodeTitle,
        string seriesTitle,
        PlayerFormatInformation? preferFormat = null,
        bool silent = false,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        try
        {
            if (!silent)
            {
                await _notificationService.ShowInAppNotificationAsync(
                    $"正在解析第 {episodeNumber} 集直链地址...",
                    NotificationType.Information
                );
            }

            IReadOnlyList<PlaybackMedia>? mediaList = await _apiClient.ResolveMediaAsync(
                dramaId,
                videoId,
                cancellationToken
            );

            if (mediaList is null || mediaList.Count == 0)
            {
                if (!silent)
                {
                    await _notificationService.ShowInAppNotificationAsync(
                        $"第 {episodeNumber} 集视频地址解析失败",
                        NotificationType.Error
                    );
                }
                return null;
            }

            // 优先匹配指定清晰度，若未指定或未匹配则选取码率/分辨率最高者
            PlaybackMedia? targetMedia = null;
            if (preferFormat is not null)
            {
                targetMedia = mediaList.FirstOrDefault(m =>
                    GetQualityScore(m) == preferFormat.Quality
                );
            }
            targetMedia ??= mediaList.OrderByDescending(GetQualityScore).FirstOrDefault();

            if (targetMedia is null || string.IsNullOrWhiteSpace(targetMedia.Url))
            {
                if (!silent)
                {
                    await _notificationService.ShowInAppNotificationAsync(
                        $"第 {episodeNumber} 集视频播放源不存在",
                        NotificationType.Error
                    );
                }
                return null;
            }

            string downloadDir = DownloadFolderPath;
            string fileName = $"{SanitizeFileName(seriesTitle)} - 第{episodeNumber}集.mp4";
            string outputPath = Path.Combine(downloadDir, fileName);
            string tempPath = Path.Combine(
                downloadDir,
                $".temp_{episodeNumber}_{Guid.NewGuid():N}.mp4"
            );

            try
            {
                if (!silent)
                {
                    await _notificationService.ShowInAppNotificationAsync(
                        $"正在高速下载第 {episodeNumber} 集数据...",
                        NotificationType.Information
                    );
                }

                // 1. 发起流式 HTTP 下载请求
                using (HttpRequestMessage request = new(HttpMethod.Get, targetMedia.Url))
                {
                    string referer = !string.IsNullOrEmpty(targetMedia.Referer)
                        ? targetMedia.Referer
                        : "https://novel.snssdk.com";
                    request.Headers.Referrer = new Uri(referer);
                    request.Headers.TryAddWithoutValidation(
                        "User-Agent",
                        "com.phoenix.read/73532 (Linux; U; Android 16; zh_CN; 25053RT47C; Build/BP2A.250605.031.A3; Cronet/TTNetVersion:04657795 2026-01-23 QuicVersion:c67e9834 2025-09-08)"
                    );

                    using HttpResponseMessage response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken
                    );
                    response.EnsureSuccessStatusCode();

                    await using (
                        FileStream fs = new(
                            tempPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None
                        )
                    )
                    {
                        await response.Content.CopyToAsync(fs, cancellationToken);
                    }
                }

                // 2. 判断是否具有 CENC AES-128 加密
                if (!string.IsNullOrWhiteSpace(targetMedia.DecryptionKey))
                {
                    if (!silent)
                    {
                        await _notificationService.ShowInAppNotificationAsync(
                            $"正在通过纯 C# 解密第 {episodeNumber} 集...",
                            NotificationType.Information
                        );
                    }

                    byte[] keyBytes = Convert.FromHexString(targetMedia.DecryptionKey);

                    // 在独立后台线程中执行极速就地解密
                    await Task.Run(
                        () =>
                        {
                            using FileStream encFs = new(
                                tempPath,
                                FileMode.Open,
                                FileAccess.ReadWrite,
                                FileShare.None,
                                bufferSize: 65536,
                                useAsync: false
                            );
                            Mp4CencDecryptor.DecryptInPlace(encFs, keyBytes, cancellationToken);
                        },
                        cancellationToken
                    );
                }

                // 3. 将临时文件移动为最终目标文件
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                File.Move(tempPath, outputPath);

                if (!silent)
                {
                    await _notificationService.ShowInAppNotificationAsync(
                        $"第 {episodeNumber} 集下载完成: {fileName}",
                        NotificationType.Success
                    );
                    _notificationService.ShowSystemNotification(
                        seriesTitle,
                        $"第 {episodeNumber} 集下载成功！"
                    );

                    if (_settingsService.Settings.OpenFolderAfterDownload)
                    {
                        TryOpenFolderAndSelect(outputPath);
                    }
                }

                return outputPath;
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch { }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.ZLogInformation($"下载第 {episodeNumber} 集任务已取消.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"下载第 {episodeNumber} 集失败: {ex.Message}");
            if (!silent)
            {
                await _notificationService.ShowInAppNotificationAsync(
                    $"下载第 {episodeNumber} 集失败: {ex.Message}",
                    NotificationType.Error
                );
            }
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task BatchDownloadEpisodesAsync(
        string dramaId,
        string seriesTitle,
        IReadOnlyList<Episode> episodes,
        PlayerFormatInformation? preferFormat = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        if (episodes is null || episodes.Count == 0)
        {
            await _notificationService.ShowInAppNotificationAsync(
                "无分集可下载",
                NotificationType.Warning
            );
            return;
        }

        List<Episode> list = [.. episodes];
        int total = list.Count;
        string tag = $"batch_download_{dramaId}";

        await _notificationService.ShowInAppNotificationAsync(
            $"开始后台批量下载 {seriesTitle} (共 {total} 集)...",
            NotificationType.Information
        );

        _notificationService.UpdateProgressNotification(
            tag,
            $"{seriesTitle} 批量下载",
            $"准备下载共 {total} 集...",
            0.0,
            isCompleted: false
        );

        int completed = 0;
        foreach (Episode ep in list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            double currentProgress = (double)completed / total;
            string status = $"正在下载 ({completed + 1}/{total}): 第{ep.Number}集 {ep.Title}";

            _notificationService.UpdateProgressNotification(
                tag,
                $"{seriesTitle} 批量下载",
                status,
                currentProgress,
                isCompleted: false
            );

            string epTitle = string.IsNullOrWhiteSpace(ep.Title) ? $"第{ep.Number}集" : ep.Title;
            string? result = await DownloadSingleEpisodeAsync(
                dramaId,
                ep.VideoId,
                ep.Number,
                epTitle,
                seriesTitle,
                preferFormat,
                silent: true,
                cancellationToken
            );

            if (result is not null)
            {
                completed++;
            }
        }

        // 最终完成通知
        _notificationService.UpdateProgressNotification(
            tag,
            $"{seriesTitle} 批量下载",
            $"全部 {completed}/{total} 集已下载完成！",
            1.0,
            isCompleted: true
        );

        await _notificationService.ShowInAppNotificationAsync(
            $"{seriesTitle} 批量下载完成，成功 {completed}/{total} 集！",
            NotificationType.Success
        );

        _notificationService.ShowSystemNotification(
            seriesTitle,
            $"批量下载任务已完成 (成功 {completed}/{total} 集)！"
        );

        if (_settingsService.Settings.OpenFolderAfterDownload)
        {
            TryOpenFolder(DownloadFolderPath);
        }
    }

    /// <inheritdoc/>
    public async Task<string?> DownloadCoverAsync(
        string coverUrl,
        string seriesTitle,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(coverUrl))
        {
            await _notificationService.ShowInAppNotificationAsync(
                "未找到封面图片地址",
                NotificationType.Warning
            );
            return null;
        }

        try
        {
            string fileName = $"{SanitizeFileName(seriesTitle)}_cover.jpg";
            string savePath = Path.Combine(DownloadFolderPath, fileName);

            byte[] bytes = await _httpClient.GetByteArrayAsync(coverUrl, cancellationToken);
            await File.WriteAllBytesAsync(savePath, bytes, cancellationToken);

            await _notificationService.ShowInAppNotificationAsync(
                $"封面已保存至: {savePath}",
                NotificationType.Success
            );

            if (_settingsService.Settings.OpenFolderAfterDownload)
            {
                TryOpenFolderAndSelect(savePath);
            }

            return savePath;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"下载封面失败.");
            await _notificationService.ShowInAppNotificationAsync(
                "下载封面失败",
                NotificationType.Error
            );
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> DownloadDanmakuAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        string seriesTitle,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(dramaId) || string.IsNullOrWhiteSpace(videoId))
        {
            await _notificationService.ShowInAppNotificationAsync(
                "无可用弹幕数据",
                NotificationType.Warning
            );
            return null;
        }

        try
        {
            DanmakuPage danmakuPage = await _apiClient.GetDanmakuAsync(
                dramaId,
                videoId,
                0,
                3600000,
                cancellationToken
            );

            string fileName = $"{SanitizeFileName(seriesTitle)}_第{episodeNumber}集_弹幕.xml";
            string savePath = Path.Combine(DownloadFolderPath, fileName);

            StringBuilder sb = new();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<i>");
            sb.AppendLine("<chatserver>phoenixread</chatserver>");
            sb.AppendLine($"<chatid>{videoId}</chatid>");
            sb.AppendLine("<mission>0</mission>");
            sb.AppendLine($"<maxlimit>{danmakuPage.Items.Count}</maxlimit>");

            foreach (DanmakuItem item in danmakuPage.Items)
            {
                double timeSec = item.OffsetMs / 1000.0;
                sb.AppendLine(
                    $"<d p=\"{timeSec:F2},1,25,16777215,0,0,0,0\">{SecurityElement.Escape(item.Text)}</d>"
                );
            }
            sb.AppendLine("</i>");

            await File.WriteAllTextAsync(savePath, sb.ToString(), Encoding.UTF8, cancellationToken);

            await _notificationService.ShowInAppNotificationAsync(
                $"弹幕已保存至: {savePath}",
                NotificationType.Success
            );

            if (_settingsService.Settings.OpenFolderAfterDownload)
            {
                TryOpenFolderAndSelect(savePath);
            }

            return savePath;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"下载弹幕失败.");
            await _notificationService.ShowInAppNotificationAsync(
                "下载弹幕失败",
                NotificationType.Error
            );
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task DecryptVideoAsync(
        string inputFilePath,
        string outputFilePath,
        string hexKey,
        CancellationToken cancellationToken = default
    )
    {
        byte[] keyBytes = Convert.FromHexString(hexKey);
        await Mp4CencDecryptor.DecryptFileAsync(
            inputFilePath,
            outputFilePath,
            keyBytes,
            cancellationToken
        );
    }

    private static int GetQualityScore(PlaybackMedia media)
    {
        if (media is { Width: > 0, Height: > 0 })
        {
            return Math.Min(media.Width, media.Height);
        }

        if (media.Quality > 0)
        {
            return media.Quality;
        }

        return (int)media.Definition;
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

    private static void TryOpenFolderAndSelect(string filePath)
    {
        try
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        catch { }
    }

    private static void TryOpenFolder(string folderPath)
    {
        try
        {
            Process.Start("explorer.exe", folderPath);
        }
        catch { }
    }

    public void Dispose()
    {
        // Don't dispose _httpClient if it was injected from outside, but clean if created locally
    }
}
