using System.Collections.Concurrent;
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
/// 短剧与媒体下载服务实现.
/// </summary>
public sealed class DownloadService(
    ILogger<DownloadService> logger,
    ISettingsService settingsService,
    INotificationService notificationService,
    PhoenixReadApiClient apiClient,
    IPlaybackMediaService playbackMediaService,
    IDanmakuService danmakuService,
    IHttpClientFactory httpClientFactory
) : IDownloadService
{
    private readonly ILogger<DownloadService> _logger = logger;
    private readonly ISettingsService _settingsService = settingsService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly PhoenixReadApiClient _apiClient = apiClient;
    private readonly IPlaybackMediaService _playbackMediaService = playbackMediaService;
    private readonly IDanmakuService _danmakuService = danmakuService;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("PhoenixReadClient");

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
        string? coverUrl = null,
        PlayerFormatInformation? preferFormat = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dramaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(videoId);

        return await Task.Run(
            async () =>
            {
                string dramaDir = GetDramaFolderPath(dramaTitle);
                string? logoUri = ResolveCoverUri(dramaDir, coverUrl);

                try
                {
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
                        _notificationService.ShowSystemNotification(
                            title: $"下载完成 {dramaTitle}",
                            message: $"第 {episodeNumber} 集下载完成！",
                            logoUri: logoUri,
                            folderPath: dramaDir
                        );
                    }
                    else
                    {
                        _notificationService.ShowSystemNotification(
                            title: $"下载失败 {dramaTitle}",
                            message: $"第 {episodeNumber} 集未获取到可用播放源",
                            logoUri: logoUri
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
                    _notificationService.ShowSystemNotification(
                        title: $"下载失败 {dramaTitle}",
                        message: $"第 {episodeNumber} 集下载失败: {ex.Message}",
                        logoUri: logoUri
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
        string? coverUrl = null,
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
        string? logoUri = ResolveCoverUri(dramaDir, coverUrl);

        // 初始原生通知：顶部大标题为 "正在下载 剧名"，进度条上的标题为 "剧名"
        _notificationService.UpdateProgressNotification(
            tag: tag,
            headerTitle: $"正在下载 {dramaTitle}",
            progressBarTitle: dramaTitle,
            status: $"准备下载共 {total} 集...",
            progress: 0.0,
            isCompleted: false,
            logoUri: logoUri
        );

        int completedCount = 0;
        ConcurrentBag<(int Episode, string Reason)> failedEpisodes = [];

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

                    _notificationService.UpdateProgressNotification(
                        tag: tag,
                        headerTitle: $"正在下载 {dramaTitle}",
                        progressBarTitle: dramaTitle,
                        status: $"已下载 {done}/{total} 集",
                        progress: progress,
                        isCompleted: false,
                        logoUri: logoUri
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

        // 批量完成后，先移除进度条通知，然后展示带“打开文件夹”按钮和封面 Logo 的完成系统通知
        await _notificationService.RemoveNotificationAsync(tag);

        if (failedEpisodes.IsEmpty)
        {
            _notificationService.ShowSystemNotification(
                title: $"下载完成 {dramaTitle}",
                message: $"全部 {total} 集已下载完成！",
                logoUri: logoUri,
                folderPath: dramaDir
            );
        }
        else
        {
            int successCount = completedCount;
            string failedEpNumbers = string.Join(
                "、",
                failedEpisodes.Select(f => f.Episode).OrderBy(n => n)
            );

            _notificationService.ShowSystemNotification(
                title: $"下载完成 {dramaTitle}",
                message: $"成功 {successCount}/{total} 集，第 {failedEpNumbers} 集下载失败。",
                logoUri: logoUri,
                folderPath: dramaDir
            );
        }
    }

    /// <summary>
    /// 内部单集下载与解密核心实现（命名为 {episodeNumber}.mp4，纯后台执行）.
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
        IReadOnlyList<PlaybackMedia>? mediaList = await _playbackMediaService.ResolveMediaAsync(
            dramaId,
            videoId,
            forceRefresh: false,
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

            // 2. 纯 C# CENC 就地解密
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
        string dramaDir = GetDramaFolderPath(dramaTitle);
        if (string.IsNullOrWhiteSpace(coverUrl))
        {
            _notificationService.ShowSystemNotification(
                title: $"封面下载失败 {dramaTitle}",
                message: "未找到封面图片地址"
            );
            return null;
        }

        try
        {
            string savePath = Path.Combine(dramaDir, "cover.jpg");
            byte[] bytes = await _httpClient.GetByteArrayAsync(coverUrl, cancellationToken);
            await File.WriteAllBytesAsync(savePath, bytes, cancellationToken);

            _notificationService.ShowSystemNotification(
                title: $"封面下载完成 {dramaTitle}",
                message: "封面已成功保存至本地！",
                logoUri: savePath,
                folderPath: dramaDir
            );

            return savePath;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"下载封面失败.");
            _notificationService.ShowSystemNotification(
                title: $"封面下载失败 {dramaTitle}",
                message: $"下载封面失败: {ex.Message}"
            );
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> DownloadDanmakuAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        string dramaTitle,
        string? coverUrl = null,
        CancellationToken cancellationToken = default
    )
    {
        string dramaDir = GetDramaFolderPath(dramaTitle);
        string? logoUri = ResolveCoverUri(dramaDir, coverUrl);

        if (string.IsNullOrWhiteSpace(dramaId) || string.IsNullOrWhiteSpace(videoId))
        {
            _notificationService.ShowSystemNotification(
                title: $"弹幕下载提示 {dramaTitle}",
                message: $"第 {episodeNumber} 集无可用弹幕数据",
                logoUri: logoUri
            );
            return null;
        }

        try
        {
            DanmakuPage danmakuPage = await _danmakuService.GetDanmakuAsync(
                dramaId,
                videoId,
                0,
                3600000,
                cancellationToken: cancellationToken
            );

            if (danmakuPage.Items is null || danmakuPage.Items.Count == 0)
            {
                _notificationService.ShowSystemNotification(
                    title: $"弹幕下载提示 {dramaTitle}",
                    message: $"第 {episodeNumber} 集无可用弹幕数据",
                    logoUri: logoUri,
                    folderPath: dramaDir
                );
                return null;
            }

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

            _notificationService.ShowSystemNotification(
                title: $"弹幕下载完成 {dramaTitle}",
                message: $"第 {episodeNumber} 集弹幕已下载完成！",
                logoUri: logoUri,
                folderPath: dramaDir
            );

            return savePath;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"下载第 {episodeNumber} 集弹幕失败.");
            _notificationService.ShowSystemNotification(
                title: $"弹幕下载失败 {dramaTitle}",
                message: $"下载第 {episodeNumber} 集弹幕失败: {ex.Message}",
                logoUri: logoUri
            );
            return null;
        }
    }

    /// <inheritdoc/>
    public Task DecryptVideoAsync(
        string inputFilePath,
        string outputFilePath,
        string hexKey,
        CancellationToken cancellationToken = default
    )
    {
        byte[] keyBytes = Convert.FromHexString(hexKey);
        Mp4CencDecryptor.DecryptFile(inputFilePath, outputFilePath, keyBytes, cancellationToken);
        return Task.CompletedTask;
    }

    private static string? ResolveCoverUri(string dramaDir, string? fallbackUrl)
    {
        string localCover = Path.Combine(dramaDir, "cover.jpg");
        if (File.Exists(localCover))
        {
            return localCover;
        }

        return !string.IsNullOrWhiteSpace(fallbackUrl) ? fallbackUrl : null;
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
}
