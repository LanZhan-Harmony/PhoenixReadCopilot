using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.ViewModels.Core;

namespace PhoenixReadCopilot.Extensions;

internal sealed partial class PlayerTemplateSelector : DataTemplateSelector
{
    public DataTemplate FFmpegTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is FFmpegPlayerViewModel)
        {
            return FFmpegTemplate;
        }

        return default;
    }
}
