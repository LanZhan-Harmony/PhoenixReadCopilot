using PhoenixReadCopilot.UI.ViewModels.Items;

namespace PhoenixReadCopilot.UI.Extensions;

/// <summary>
/// 搜索建议模板选择器.
/// </summary>
internal sealed partial class SearchSuggestTemplateSelector : DataTemplateSelector
{
    public DataTemplate BasicTemplate { get; set; }

    public DataTemplate RegionTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        var data = item as SearchSuggestItemViewModel;
        return string.IsNullOrEmpty(data.RegionId) ? BasicTemplate : RegionTemplate;
    }
}
