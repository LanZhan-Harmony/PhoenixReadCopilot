namespace PhoenixReadCopilot.Core.Models.Subtitle;

/// <summary>
/// 字幕内容信息模型.
/// </summary>
public sealed class SubtitleInformation(
    string id,
    string language,
    string? url = null,
    int startPosition = 0,
    int endPosition = 0,
    string content = ""
)
{
    /// <summary>
    /// 字幕唯一标识.
    /// </summary>
    public string Id { get; init; } = id;

    /// <summary>
    /// 语言名称或代号.
    /// </summary>
    public string Language { get; init; } = language;

    /// <summary>
    /// 字幕文件链接.
    /// </summary>
    public string Url { get; init; } = url ?? string.Empty;

    /// <summary>
    /// 起始位置（秒或毫秒）.
    /// </summary>
    public int StartPosition { get; init; } = startPosition;

    /// <summary>
    /// 结束位置（秒或毫秒）.
    /// </summary>
    public int EndPosition { get; init; } = endPosition;

    /// <summary>
    /// 字幕文本内容.
    /// </summary>
    public string Content { get; init; } = content;
}
