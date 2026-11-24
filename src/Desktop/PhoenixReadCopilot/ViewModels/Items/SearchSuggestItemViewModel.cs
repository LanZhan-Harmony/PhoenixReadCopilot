using CommunityToolkit.Mvvm.ComponentModel;
using PhoenixReadCopilot.Core.Models.Search;

namespace PhoenixReadCopilot.ViewModels.Items;

/// <summary>
/// 搜索建议项视图模型.
/// </summary>
internal sealed partial class SearchSuggestItemViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string SearchContent { get; set; }

    [ObservableProperty]
    public partial string? RegionName { get; set; }

    public SearchSuggestItemViewModel(SearchSuggestItem item)
    {
        Keyword = item.Keyword;
        SearchContent = item.Text;
    }

    public SearchSuggestItemViewModel(string keyword)
    {
        Keyword = keyword;
        SearchContent = keyword;
    }

    public SearchSuggestItemViewModel(string regionId, string regionName)
    {
        RegionId = regionId;
        RegionName = regionName;
        Keyword = string.Empty;
        SearchContent = string.Empty;
    }

    /// <summary>
    /// 关键词.
    /// </summary>
    public string Keyword { get; set; }

    /// <summary>
    /// 区域标识.
    /// </summary>
    public string? RegionId { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is SearchSuggestItemViewModel model
        && Keyword == model.Keyword
        && RegionId == model.RegionId;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Keyword, RegionId);
}
