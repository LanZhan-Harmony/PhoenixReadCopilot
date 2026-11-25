using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class PlayerSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate? CommentTemplate { get; set; }

    public DataTemplate? DramaReviewTemplate { get; set; }

    public DataTemplate? RecommendTemplate { get; set; }

    public DataTemplate? PartTemplate { get; set; }

    public DataTemplate? InfoTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        DataTemplate? template = item switch
        {
            DramaReviewSectionDetailViewModel => DramaReviewTemplate,
            CommentMainViewModel => CommentTemplate,
            VideoPlayerRecommendSectionDetailViewModel => RecommendTemplate,
            VideoPlayerPartSectionDetailViewModel => PartTemplate,
            VideoPlayerInfoSectionDetailViewModel => InfoTemplate,
            _ => null,
        };

        return template ?? base.SelectTemplateCore(item, container);
    }
}
