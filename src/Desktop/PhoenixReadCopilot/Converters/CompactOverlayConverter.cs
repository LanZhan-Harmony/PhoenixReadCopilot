using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Strings;
using Symbol = FluentIcons.Common.Symbol;

namespace PhoenixReadCopilot.Converters;

/// <summary>
/// 全屏图标及文本转换器.
/// </summary>
internal sealed partial class CompactOverlayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isCompact = (bool)value;

        if (targetType == typeof(Symbol))
        {
            return isCompact ? Symbol.ContractDownLeft : Symbol.ContractUpRight;
        }
        else
        {
            return isCompact ? Resources.ExitCompactOverlay : Resources.EnterCompactOverlay;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
