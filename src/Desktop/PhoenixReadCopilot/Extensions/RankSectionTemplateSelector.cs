using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

/// <summary>
/// 榜单主内容数据模板选择器.
/// </summary>
internal sealed partial class RankSectionTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// 短剧榜单模板.
    /// </summary>
    public DataTemplate? DramaTemplate { get; set; }

    /// <summary>
    /// 演员榜单模板.
    /// </summary>
    public DataTemplate? ActorTemplate { get; set; }

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        => SelectTemplateCore(item);

    /// <inheritdoc/>
    protected override DataTemplate? SelectTemplateCore(object item)
    {
        if (item is RankSectionItemViewModel section)
        {
            return section.IsActorLeaderboard ? ActorTemplate : DramaTemplate;
        }

        return DramaTemplate;
    }
}
