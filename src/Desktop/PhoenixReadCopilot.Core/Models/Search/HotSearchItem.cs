namespace PhoenixReadCopilot.Core.Models.Search;

/// <summary>
/// 热搜条目模型.
/// </summary>
public sealed class HotSearchItem
{
    /// <summary>
    /// 排名序号.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 展示文本.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 搜索关键词.
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// 图标.
    /// </summary>
    public SearchItemIcon? Icon { get; set; }

    /// <summary>
    /// 标签 (如“新剧”、“热播”).
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// 热度文案.
    /// </summary>
    public string? Heat { get; set; }
}
