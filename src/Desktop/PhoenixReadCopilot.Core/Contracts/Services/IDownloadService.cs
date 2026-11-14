using PhoenixReadApi;
using PhoenixReadCopilot.Core.Models.Player;

namespace PhoenixReadCopilot.Core.Contracts.Services;

/// <summary>
/// 短剧与媒体下载服务契约.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// 获取当前配置的本地下载保存目录.
    /// </summary>
    string DownloadFolderPath { get; }

    /// <summary>
    /// 下载单个分集视频，并在需要时进行纯 C# CENC 解密.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">视频 ID.</param>
    /// <param name="episodeNumber">集数序号.</param>
    /// <param name="episodeTitle">分集标题.</param>
    /// <param name="seriesTitle">剧集总标题.</param>
    /// <param name="preferFormat">首选清晰度格式信息（若为空则自动选择最高画质）.</param>
    /// <param name="silent">是否静默下载（不频繁弹窗提示开始下载）.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>下载成功后的本地绝对路径，若失败则返回 null.</returns>
    Task<string?> DownloadSingleEpisodeAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        string episodeTitle,
        string seriesTitle,
        PlayerFormatInformation? preferFormat = null,
        bool silent = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 批量下载多个分集并在通知中报告进度.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="seriesTitle">剧集总标题.</param>
    /// <param name="episodes">待下载的分集列表.</param>
    /// <param name="preferFormat">首选清晰度格式信息.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task BatchDownloadEpisodesAsync(
        string dramaId,
        string seriesTitle,
        IReadOnlyList<Episode> episodes,
        PlayerFormatInformation? preferFormat = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 下载封面图.
    /// </summary>
    /// <param name="coverUrl">封面图片地址.</param>
    /// <param name="seriesTitle">剧集标题.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>保存的本地文件路径.</returns>
    Task<string?> DownloadCoverAsync(
        string coverUrl,
        string seriesTitle,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 下载分集弹幕并导出为标准 XML 文件.
    /// </summary>
    /// <param name="dramaId">短剧 ID.</param>
    /// <param name="videoId">视频 ID.</param>
    /// <param name="episodeNumber">集数序号.</param>
    /// <param name="seriesTitle">剧集标题.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>保存的本地文件路径.</returns>
    Task<string?> DownloadDanmakuAsync(
        string dramaId,
        string videoId,
        int episodeNumber,
        string seriesTitle,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 使用纯 C# 在独立后台线程中解密包含 CENC AES-128 加密的 MP4 视频文件.
    /// </summary>
    /// <param name="inputFilePath">待解密的 MP4 视频文件路径.</param>
    /// <param name="outputFilePath">解密后的输出文件路径.</param>
    /// <param name="hexKey">16 字节 AES-128 密钥十六进制字符串.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    Task DecryptVideoAsync(
        string inputFilePath,
        string outputFilePath,
        string hexKey,
        CancellationToken cancellationToken = default
    );
}
