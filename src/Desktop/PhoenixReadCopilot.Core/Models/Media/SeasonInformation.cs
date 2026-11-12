namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 剧集/赛季信息模型.
/// </summary>
public sealed class SeasonInformation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeasonInformation"/> class.
    /// </summary>
    public SeasonInformation(MediaIdentifier identifier)
    {
        Identifier = identifier;
    }

    /// <summary>
    /// 基础媒体标识.
    /// </summary>
    public MediaIdentifier Identifier { get; init; }
}
