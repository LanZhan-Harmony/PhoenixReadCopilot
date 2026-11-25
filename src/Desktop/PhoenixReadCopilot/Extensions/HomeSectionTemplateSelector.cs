using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

/// <summary>
/// 首页分区内容数据模板选择器.
/// </summary>
internal sealed partial class HomeSectionTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// 推荐短剧模板.
    /// </summary>
    public DataTemplate? RecommendTemplate { get; set; }

    /// <summary>
    /// 上新短剧模板.
    /// </summary>
    public DataTemplate? NewArrivalTemplate { get; set; }

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        => SelectTemplateCore(item);

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is HomeSectionItemViewModel section)
        {
            return section.SectionType switch
            {
                HomeSectionType.Recommend => RecommendTemplate,
                HomeSectionType.NewArrival => NewArrivalTemplate,
                _ => RecommendTemplate,
            };
        }

        return RecommendTemplate;
    }
}
