using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.Core.Enums;
using PhoenixReadCopilot.ViewModels.Components;
using PhoenixReadCopilot.ViewModels.Items;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class MomentSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoTemplate { get; set; }

    public DataTemplate ComprehensiveTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is UserMomentDetailViewModel momentDetailVM)
        {
            return momentDetailVM.IsVideo() ? VideoTemplate : ComprehensiveTemplate;
        }

        if (item is not IMomentSectionDetailViewModel)
        {
            return null;
        }

        IMomentSectionDetailViewModel vm = (IMomentSectionDetailViewModel)item;
        return vm.SectionType == MomentSectionType.Video ? VideoTemplate : ComprehensiveTemplate;
    }
}
