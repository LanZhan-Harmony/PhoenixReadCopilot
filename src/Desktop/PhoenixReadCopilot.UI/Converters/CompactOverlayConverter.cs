using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;

namespace PhoenixReadCopilot.UI.Converters;

/// <summary>
/// 全屏图标及文本转换器.
/// </summary>
internal sealed partial class CompactOverlayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isCompact = (bool)value;

        if (targetType == typeof(FluentIcons.Common.Symbol))
        {
            return isCompact
                ? FluentIcons.Common.Symbol.ContractDownLeft
                : FluentIcons.Common.Symbol.ContractUpRight;
        }
        else
        {
            return isCompact
                ? ResourceToolkit.GetLocalizedString(StringNames.ExitCompactOverlay)
                : ResourceToolkit.GetLocalizedString(StringNames.EnterCompactOverlay);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
