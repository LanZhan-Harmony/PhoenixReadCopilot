using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

/// <summary>
/// 视频卡片模板选择器.
/// </summary>
internal sealed partial class VideoCardTemplateSelector : DataTemplateSelector
{
    public DataTemplate RecommendTemplate { get; set; }

    public DataTemplate HotTemplate { get; set; }

    public DataTemplate RankTemplate { get; set; }

    public DataTemplate? ActorTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return SelectTemplateCore(item, null!);
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is VideoItemViewModel vm)
        {
            if (vm.Actor is not null && ActorTemplate is not null)
            {
                return ActorTemplate;
            }

            return vm.Style switch
            {
                VideoCardStyle.Recommend => RecommendTemplate,
                VideoCardStyle.Hot => HotTemplate,
                VideoCardStyle.Rank => RankTemplate,
                _ => RecommendTemplate,
            };
        }

        return base.SelectTemplateCore(item, container);
    }
}
