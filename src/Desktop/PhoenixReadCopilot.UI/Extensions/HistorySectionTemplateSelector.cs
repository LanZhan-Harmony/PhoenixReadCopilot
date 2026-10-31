using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.UI.ViewModels.Items;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.Extensions;

internal sealed partial class HistorySectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoTemplate { get; set; }

    public DataTemplate ArticleTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is IHistorySectionDetailViewModel vm)
        {
            return vm.Type switch
            {
                ViewHistoryTabType.Video => VideoTemplate,
                ViewHistoryTabType.Article => ArticleTemplate,
                _ => throw new ArgumentOutOfRangeException(nameof(vm.Type), vm.Type, null),
            };
        }

        return default;
    }
}
