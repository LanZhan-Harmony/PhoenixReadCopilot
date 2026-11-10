namespace PhoenixReadCopilot.Models.Search;

/// <summary>
/// 热搜/搜索条目图标.
/// </summary>
public sealed class SearchItemIcon
{
    /// <summary>
    /// 图标地址.
    /// </summary>
    public Uri? Uri { get; set; }
}

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

/// <summary>
/// 搜索建议条目.
/// </summary>
public class SearchSuggestItem
{
    /// <summary>
    /// 搜索关键词.
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// 建议文本.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSuggestItem"/> class.
    /// </summary>
    public SearchSuggestItem() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSuggestItem"/> class.
    /// </summary>
    public SearchSuggestItem(string keyword, string text)
    {
        Keyword = keyword;
        Text = text;
    }
}

/// <summary>
/// 搜索分区模型.
/// </summary>
public record SearchPartition(int Id, string Title);

