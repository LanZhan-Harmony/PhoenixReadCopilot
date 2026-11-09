using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

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

        return null;
    }
}
