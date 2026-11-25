namespace PhoenixReadCopilot.Core.Models.Media;

/// <summary>
/// 分集信息模型.
/// </summary>
public sealed class EpisodeInformation(
    MediaIdentifier identifier,
    double? duration = null,
    int? index = null,
    string? episodeId = null
)
{
    /// <summary>
    /// 基础媒体标识.
    /// </summary>
    public MediaIdentifier Identifier { get; init; } = identifier;

    /// <summary>
    /// 时长（秒）.
    /// </summary>
    public double? Duration { get; init; } = duration;

    /// <summary>
    /// 集数序号.
    /// </summary>
    public int? Index { get; init; } = index;

    /// <summary>
    /// 分集业务标识.
    /// </summary>
    public string? EpisodeId { get; init; } = episodeId;

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
