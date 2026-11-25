using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

/// <summary>
/// 搜索建议模板选择器.
/// </summary>
internal sealed partial class SearchSuggestTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BasicTemplate { get; set; }

    public DataTemplate? RegionTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        DataTemplate? template =
            item is SearchSuggestItemViewModel data && !string.IsNullOrEmpty(data.RegionId)
                ? RegionTemplate
                : BasicTemplate;
        return template ?? base.SelectTemplateCore(item, container);
    }
}
