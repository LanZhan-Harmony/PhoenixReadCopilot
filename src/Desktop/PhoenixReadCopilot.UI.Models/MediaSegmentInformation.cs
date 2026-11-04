namespace PhoenixReadCopilot.UI.Models;

/// <summary>
/// 媒体分段信息.
/// </summary>
public sealed class MediaSegmentInformation
{
    /// <summary>
    /// 分段标识符.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 基础 URL.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// 带宽.
    /// </summary>
    public long Bandwidth { get; init; }

    /// <summary>
    /// 编码格式.
    /// </summary>
    public string Codecs { get; init; } = string.Empty;

    /// <summary>
    /// MIME 类型.
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// 宽度.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// 高度.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// 索引范围.
    /// </summary>
    public string IndexRange { get; init; } = string.Empty;

    /// <summary>
    /// 初始化范围.
    /// </summary>
    public string Initialization { get; init; } = string.Empty;
}
