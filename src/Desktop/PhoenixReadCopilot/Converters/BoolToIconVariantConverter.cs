using Microsoft.UI.Xaml.Data;

namespace PhoenixReadCopilot.Converters;

/// <summary>
/// 图标变体转换器.
/// </summary>
internal sealed partial class BoolToIconVariantConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null)
        {
            return FluentIcons.Common.IconVariant.Regular;
        }

        bool isFilled = (bool)value;
        return isFilled
            ? FluentIcons.Common.IconVariant.Filled
            : FluentIcons.Common.IconVariant.Regular;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
