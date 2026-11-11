namespace PhoenixReadCopilot.Models.Search;

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
