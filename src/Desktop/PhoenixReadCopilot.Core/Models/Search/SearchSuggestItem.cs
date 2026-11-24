namespace PhoenixReadCopilot.Core.Models.Search;

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

    public SearchSuggestItem() { }

    public SearchSuggestItem(string keyword, string text)
    {
        Keyword = keyword;
        Text = text;
    }
}
