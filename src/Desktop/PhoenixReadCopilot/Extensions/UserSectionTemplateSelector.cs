using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadApi;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

/// <summary>
/// 用户空间主体内容数据模板选择器（基于选中的 Section 或 SubTab 纯 MVVM 自动选择对应视图模板）.
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

    /// <summary>
    /// 他人作品待播预约卡片展示模板（模仿上新日历待播卡片风格）.
    /// </summary>
    public DataTemplate? ReservationTemplate { get; set; }

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
                "Reviews" => ReviewTemplate,
                "Circle" => CircleTemplate,
                "Follows" or "Fans" => RelationTemplate,
                // 个人主页自己的 Reservation、History、Favorite、ViewLater、Likes 均使用 VideoTemplate
                _ => VideoTemplate,
            };
        }

        if (item is UserSpaceSubTabItemViewModel subTab)
        {
            if (subTab.Value is UserWorkSubTab workTab)
            {
                return workTab switch
                {
                    UserWorkSubTab.Review => ReviewTemplate,
                    UserWorkSubTab.Reservation => ReservationTemplate ?? VideoTemplate,
                    _ => VideoTemplate,
                };
            }

            return VideoTemplate;
        }

        return VideoTemplate;
    }
}
