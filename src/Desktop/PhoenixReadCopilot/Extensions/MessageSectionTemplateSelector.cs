using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Components;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class MessageSectionTemplateSelector : DataTemplateSelector
{
    public DataTemplate NotifyTemplate { get; set; }

    public DataTemplate ChatTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is NotifyMessageSectionDetailViewModel)
        {
            return NotifyTemplate;
        }
        else if (item is ChatMessageSectionDetailViewModel)
        {
            return ChatTemplate;
        }

        return null;
    }
}
