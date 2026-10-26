using PhoenixReadCopilot.UI.ViewModels.Core;

namespace PhoenixReadCopilot.UI.Extensions;

internal sealed partial class PlayerTemplateSelector : DataTemplateSelector
{
    public DataTemplate MpvTemplate { get; set; }

    public DataTemplate NativeTemplate { get; set; }

    public DataTemplate IslandTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is NativePlayerViewModel)
        {
            return NativeTemplate;
        }
        else if (item is IslandPlayerViewModel)
        {
            return IslandTemplate;
        }

        return default;
    }
}
