using PhoenixReadCopilot.UI.ViewModels.Items;

namespace PhoenixReadCopilot.UI.Extensions;

internal sealed partial class PopularSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate SectionTemplate { get; set; }

    public DataTemplate PartitionTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is PopularSectionItemViewModel)
        {
            return SectionTemplate;
        }
        else if (item is PopularRankPartitionViewModel)
        {
            return PartitionTemplate;
        }

        return default;
    }
}
