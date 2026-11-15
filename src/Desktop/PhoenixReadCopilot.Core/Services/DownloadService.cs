using System.Collections.Concurrent;
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
/// 短剧与媒体下载服务实现 (基于纯 C# CENC 就地极速解密引擎，高度并行化下载).
/// </summary>
public sealed class DownloadService : IDownloadService
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
        IHttpClientFactory httpClientFactory
    )
    {
        _logger = logger;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _apiClient = apiClient;
        _httpClient = httpClientFactory.CreateClient("PhoenixReadClient");
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

    /// <summary>
    /// 获取并确保某部短剧专属下载目录存在.
    /// </summary>
    public string GetDramaFolderPath(string dramaTitle)
    {
        string sanitized = SanitizeFileName(dramaTitle);
        string dramaDir = Path.Combine(DownloadFolderPath, sanitized);
        if (!Directory.Exists(dramaDir))
        {
            Directory.CreateDirectory(dramaDir);
        }
        return dramaDir;
    }

    /// <inheritdoc/>
    public async Task<string?> DownloadSingleEpisodeAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        string episodeTitle,
        string dramaTitle,
        PlayerFormatInformation? preferFormat = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        return await Task.Run(
            async () =>
            {
                try
                {
                    string dramaDir = GetDramaFolderPath(dramaTitle);
                    string? outputPath = await DownloadSingleEpisodeCoreAsync(
                        dramaId,
                        videoId,
                        episodeNumber,
                        dramaDir,
                        preferFormat,
                        cancellationToken
                    );

                    if (outputPath is not null)
                    {
                        // 单集成功时使用原生系统通知，不弹应用内提示
                        _notificationService.ShowSystemNotification(
                            dramaTitle,
                            $"第 {episodeNumber} 集下载完成！"
                        );

                        if (_settingsService.Settings.OpenFolderAfterDownload)
                        {
                            TryOpenFolderAndSelect(outputPath);
                        }
                    }
                    else
                    {
                        await _notificationService.ShowInAppNotificationAsync(
                            $"第 {episodeNumber} 集下载失败: 未获取到可用播放源",
                            NotificationType.Error
                        );
                    }

                    return outputPath;
                }
                catch (OperationCanceledException)
                {
                    _logger.ZLogInformation($"下载第 {episodeNumber} 集任务已取消.");
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, $"下载第 {episodeNumber} 集失败: {ex.Message}");
                    await _notificationService.ShowInAppNotificationAsync(
                        $"第 {episodeNumber} 集下载失败: {ex.Message}",
                        NotificationType.Error
                    );
                    return null;
                }
            },
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task BatchDownloadEpisodesAsync(
        string dramaId,
        string dramaTitle,
        IReadOnlyList<Episode> episodes,
        PlayerFormatInformation? preferFormat = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        if (episodes is null || episodes.Count == 0)
        {
            return;
        }

        List<Episode> list = [.. episodes];
        int total = list.Count;
        string tag = $"batch_download_{dramaId}";
        string dramaDir = GetDramaFolderPath(dramaTitle);

        // 初始原生通知
        _notificationService.UpdateProgressNotification(
            tag,
            $"{dramaTitle} 批量下载",
            $"下载进度: 0/{total} (0%)",
            0.0,
            isCompleted: false
        );

        int completedCount = 0;
        ConcurrentBag<(int Episode, string Reason)> failedEpisodes = [];

        // 高度并行化下载
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount * 2, 4, 16),
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(list, parallelOptions, async (ep, ct) =>
        {
            try
            {
                string? result = await DownloadSingleEpisodeCoreAsync(
                    dramaId,
                    ep.VideoId,
                    ep.Number,
                    dramaDir,
                    preferFormat,
                    ct
                );

                if (result is not null)
                {
                    int done = Interlocked.Increment(ref completedCount);
                    double progress = (double)done / total;

                    // 仅当整集完全下载并解密完成后，才更新一次系统进度通知
                    _notificationService.UpdateProgressNotification(
                        tag,
                        $"{dramaTitle} 批量下载",
                        $"下载进度: {done}/{total} ({Math.Round(progress * 100)}%)",
                        progress,
                        isCompleted: false
                    );
                }
                else
                {
                    failedEpisodes.Add((ep.Number, "无法获取视频播放源"));
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, $"第 {ep.Number} 集下载异常: {ex.Message}");
                failedEpisodes.Add((ep.Number, ex.Message));
            }
        });

        // 最终通知结算
        if (failedEpisodes.IsEmpty)
        {
            // 全部成功：仅用原生系统通知，无需应用内提示
            _notificationService.UpdateProgressNotification(
                tag,
                $"{dramaTitle} 批量下载",
                $"全部 {total} 集已下载完成！",
                1.0,
                isCompleted: true
            );

            _notificationService.ShowSystemNotification(dramaTitle, $"全部 {total} 集已下载完成！");
        }
        else
        {
            // 存在失败分集：更新系统通知并统一在应用内汇总报错
            int successCount = completedCount;
            _notificationService.UpdateProgressNotification(
                tag,
                $"{dramaTitle} 批量下载",
                $"下载完成: 成功 {successCount}/{total} 集",
                1.0,
                isCompleted: true
            );

            _notificationService.ShowSystemNotification(
                dramaTitle,
                $"批量下载完成: 成功 {successCount}/{total} 集，失败 {failedEpisodes.Count} 集。"
            );

            string failedEpNumbers = string.Join(
                "、",
                failedEpisodes.Select(f => f.Episode).OrderBy(n => n)
            );

            await _notificationService.ShowInAppNotificationAsync(
                $"《{dramaTitle}》批量下载完成，但第 {failedEpNumbers} 集下载失败，请稍后重试。",
                NotificationType.Warning
            );
        }

        if (_settingsService.Settings.OpenFolderAfterDownload)
        {
            TryOpenFolder(dramaDir);
        }
    }

    /// <summary>
    /// 内部单集下载与解密核心实现.
    /// </summary>
    private async Task<string?> DownloadSingleEpisodeCoreAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        string dramaDir,
        PlayerFormatInformation? preferFormat,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<PlaybackMedia>? mediaList = await _apiClient.ResolveMediaAsync(
            dramaId,
            videoId,
            cancellationToken
        );

        if (mediaList is null || mediaList.Count == 0)
        {
            return null;
        }

        PlaybackMedia? targetMedia = null;
        if (preferFormat is not null)
        {
            targetMedia = mediaList.FirstOrDefault(m => GetQualityScore(m) == preferFormat.Quality);
        }
        targetMedia ??= mediaList.OrderByDescending(GetQualityScore).FirstOrDefault();

        if (targetMedia is null || string.IsNullOrWhiteSpace(targetMedia.Url))
        {
            return null;
        }

        // 文件命名规范: {episodeNumber}.mp4 (如 1.mp4, 2.mp4)
        string outputPath = Path.Combine(dramaDir, $"{episodeNumber}.mp4");
        string tempPath = Path.Combine(dramaDir, $".temp_{episodeNumber}_{Guid.NewGuid():N}.mp4");

        try
        {
            // 1. 流式网络下载
            using (HttpRequestMessage request = new(HttpMethod.Get, targetMedia.Url))
            {
                string referer = !string.IsNullOrEmpty(targetMedia.Referer)
                    ? targetMedia.Referer
                    : "https://novel.snssdk.com";
                request.Headers.Referrer = new Uri(referer);
                request.Headers.TryAddWithoutValidation(
                    "User-Agent",
                    "com.phoenix.read/69132 (Linux; U; Android 16; zh_CN; WKL-AN20; Build/CP2A.260605.012; Cronet/TTNetVersion:04657795 2026-01-23 QuicVersion:c67e9834 2025-09-08)"
                );

                using HttpResponseMessage response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );
                response.EnsureSuccessStatusCode();

                await using FileStream fs = new(
                    tempPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    FileOptions.Asynchronous
                );
                await response.Content.CopyToAsync(fs, cancellationToken);
            }

            // 2. CENC 就地解密 (直接在当前并行 Worker 线程执行)
            if (!string.IsNullOrWhiteSpace(targetMedia.DecryptionKey))
            {
                byte[] keyBytes = Convert.FromHexString(targetMedia.DecryptionKey);
                using FileStream encFs = new(
                    tempPath,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 65536,
                    useAsync: false
                );

                Mp4CencDecryptor.DecryptInPlace(encFs, keyBytes, cancellationToken);
            }

            // 3. 移动临时文件为正式目标分集文件
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            File.Move(tempPath, outputPath);

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

    /// <inheritdoc/>
    public async Task<string?> DownloadCoverAsync(
        string coverUrl,
        string dramaTitle,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(coverUrl))
        {
            return null;
        }

        try
        {
            string dramaDir = GetDramaFolderPath(dramaTitle);
            string savePath = Path.Combine(dramaDir, "cover.jpg");

            byte[] bytes = await _httpClient.GetByteArrayAsync(coverUrl, cancellationToken);
            await File.WriteAllBytesAsync(savePath, bytes, cancellationToken);

            if (_settingsService.Settings.OpenFolderAfterDownload)
            {
                TryOpenFolderAndSelect(savePath);
            }

            return savePath;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"下载封面失败.");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> DownloadDanmakuAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        string dramaTitle,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(dramaId) || string.IsNullOrWhiteSpace(videoId))
        {
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

            if (danmakuPage.Items is null || danmakuPage.Items.Count == 0)
            {
                return null;
            }

            string dramaDir = GetDramaFolderPath(dramaTitle);
            string savePath = Path.Combine(dramaDir, $"{episodeNumber}.xml");

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

            if (_settingsService.Settings.OpenFolderAfterDownload)
            {
                TryOpenFolderAndSelect(savePath);
            }

            return savePath;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"下载第 {episodeNumber} 集弹幕失败.");
            return null;
        }
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
}
