using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

/// <summary>
/// 用户空间主体内容数据模板选择器（直接基于选中的 Section 选择对应视图模板）.
/// </summary>
internal sealed partial class UserSectionTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// 短剧网格展示模板.
    /// </summary>
    public DataTemplate? VideoTemplate { get; set; }

    /// <summary>
    /// 剧评列表展示模板.
    /// </summary>
    public DataTemplate? ReviewTemplate { get; set; }

    /// <summary>
    /// 红果圈动态展示模板.
    /// </summary>
    public DataTemplate? CircleTemplate { get; set; }

    /// <summary>
    /// 关注与粉丝用户列表模板.
    /// </summary>
    public DataTemplate? RelationTemplate { get; set; }

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        => SelectTemplateCore(item);

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is UserSpaceSectionItemViewModel section)
        {
            return section.Id switch
            {
                "Works" => section.SelectedSubTab?.Value is UserWorkSubTab.Review ? ReviewTemplate : VideoTemplate,
                "Reviews" => ReviewTemplate,
                "Circle" => CircleTemplate,
                "Follows" or "Fans" => RelationTemplate,
                _ => VideoTemplate,
            };
        }

        return VideoTemplate;
    }
}
