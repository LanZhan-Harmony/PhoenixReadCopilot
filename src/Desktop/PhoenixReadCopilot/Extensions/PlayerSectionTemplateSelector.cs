using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class PlayerSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate CommentTemplate { get; set; }

    public DataTemplate DramaReviewTemplate { get; set; }

    public DataTemplate RecommendTemplate { get; set; }

    public DataTemplate PartTemplate { get; set; }

    public DataTemplate InfoTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is DramaReviewSectionDetailViewModel)
        {
            return DramaReviewTemplate;
        }
        else if (item is CommentMainViewModel)
        {
            return CommentTemplate;
        }
        else if (item is VideoPlayerRecommendSectionDetailViewModel)
        {
            return RecommendTemplate;
        }
        else if (item is VideoPlayerPartSectionDetailViewModel)
        {
            return PartTemplate;
        }
        else if (item is VideoPlayerInfoSectionDetailViewModel)
        {
            return InfoTemplate;
        }

        return null;
    }
}
