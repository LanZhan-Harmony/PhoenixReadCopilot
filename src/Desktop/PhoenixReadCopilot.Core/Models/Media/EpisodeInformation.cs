namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 分集信息模型.
/// </summary>
public sealed class EpisodeInformation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeInformation"/> class.
    /// </summary>
    public EpisodeInformation(
        MediaIdentifier identifier,
        double? duration = null,
        int? index = null,
        string? episodeId = null
    )
    {
        Identifier = identifier;
        Duration = duration;
        Index = index;
        EpisodeId = episodeId;
    }

    /// <summary>
    /// 基础媒体标识.
    /// </summary>
    public MediaIdentifier Identifier { get; init; }

    /// <summary>
    /// 时长（秒）.
    /// </summary>
    public double? Duration { get; init; }

    /// <summary>
    /// 集数序号.
    /// </summary>
    public int? Index { get; init; }

    /// <summary>
    /// 分集业务标识.
    /// </summary>
    public string? EpisodeId { get; init; }

    /// <summary>
    /// 扩展数据字典.
    /// </summary>
    public Dictionary<string, object> ExtensionData { get; init; } = [];

    /// <summary>
    /// 获取扩展数据.
    /// </summary>
    public T? GetExtensionIfNotNull<T>(string key)
    {
        if (ExtensionData.TryGetValue(key, out object? value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }
}
