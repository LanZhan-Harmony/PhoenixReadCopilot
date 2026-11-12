namespace PhoenixReadCopilot.Core.Models.Subtitle;

/// <summary>
/// 字幕元数据模型.
/// </summary>
public sealed class SubtitleMeta
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleMeta"/> class.
    /// </summary>
    public SubtitleMeta(
        string id,
        string language,
        string? languageDescription = null,
        bool isAI = false
    )
    {
        Id = id;
        Language = language;
        LanguageDescription = languageDescription ?? language;
        IsAI = isAI;
    }

    /// <summary>
    /// 唯一标识.
    /// </summary>
    public string Id { get; init; }

    /// <summary>
    /// 语言代号.
    /// </summary>
    public string Language { get; init; }

    /// <summary>
    /// 语言描述.
    /// </summary>
    public string LanguageDescription { get; init; }

    /// <summary>
    /// 语言名称 (兼容别名).
    /// </summary>
    public string LanguageName => LanguageDescription;

    /// <summary>
    /// 是否为 AI 生成字幕.
    /// </summary>
    public bool IsAI { get; init; }
}
