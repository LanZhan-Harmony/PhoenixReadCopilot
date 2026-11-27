using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Contracts.ViewModels;
using PhoenixReadCopilot.Core.Enums;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class HistorySectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DramaTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is IHistorySectionDetailViewModel vm)
        {
            DataTemplate? template = vm.Type switch
            {
                ViewHistoryTabType.Video => DramaTemplate,
                _ => throw new ArgumentOutOfRangeException(nameof(vm.Type), vm.Type, null),
            };
            if (template is not null)
            {
                return template;
            }
        }

        return base.SelectTemplateCore(item, container);
    }
}
