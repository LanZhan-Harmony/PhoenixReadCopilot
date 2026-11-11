using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class SearchSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoTemplate { get; set; }

    public DataTemplate PgcTemplate { get; set; }

    public DataTemplate UserTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is ISearchSectionDetailViewModel searchSectionDetailViewModel)
        {
            return searchSectionDetailViewModel.SectionType switch
            {
                SearchSectionType.Video => VideoTemplate,
                SearchSectionType.Anime or SearchSectionType.Cinema => PgcTemplate,
                SearchSectionType.User => UserTemplate,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(searchSectionDetailViewModel.SectionType),
                    searchSectionDetailViewModel.SectionType,
                    null
                ),
            };
        }

        return null;
    }
}
