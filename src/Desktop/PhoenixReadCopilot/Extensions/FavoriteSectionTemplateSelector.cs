using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class FavoriteSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate PgcTemplate { get; set; }

    public DataTemplate VideoTempalte { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is PgcFavoriteSectionDetailViewModel)
        {
            return PgcTemplate;
        }
        else if (item is VideoFavoriteSectionDetailViewModel)
        {
            return VideoTempalte;
        }

        return null;
    }
}
