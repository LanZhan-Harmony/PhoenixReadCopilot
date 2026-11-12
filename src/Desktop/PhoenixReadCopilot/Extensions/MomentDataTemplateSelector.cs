using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Models.Appearance;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class MomentDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoTemplate { get; set; }

    public DataTemplate ForwardTemplate { get; set; }

    public DataTemplate ImageTemplate { get; set; }

    public DataTemplate NotSupportTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is MomentItemViewModel)
        {
            return ForwardTemplate;
        }
        else if (item is VideoItemViewModel)
        {
            return VideoTemplate;
        }
        else if (item is IEnumerable<AppImage>)
        {
            return ImageTemplate;
        }

        return NotSupportTemplate;
    }
}
