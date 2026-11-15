using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class RankSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate SectionTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is RankSectionItemViewModel)
        {
            return SectionTemplate;
        }

        return null;
    }
}
