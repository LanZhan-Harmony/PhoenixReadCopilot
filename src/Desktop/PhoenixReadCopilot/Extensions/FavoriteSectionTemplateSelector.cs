using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class FavoriteSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate? VideoTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is VideoFavoriteSectionDetailViewModel)
        {
            return VideoTemplate;
        }

        return base.SelectTemplateCore(item, container);
    }
}
