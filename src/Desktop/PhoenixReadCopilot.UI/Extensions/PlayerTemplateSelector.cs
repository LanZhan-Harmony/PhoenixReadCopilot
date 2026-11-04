using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoenixReadCopilot.UI.ViewModels.Core;

namespace PhoenixReadCopilot.UI.Extensions;

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
