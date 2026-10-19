using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Models.Constants;
using PhoenixReadCopilot.UI.Toolkits;
using Richasy.BiliKernel.Models;

namespace PhoenixReadCopilot.UI.Converters;

/// <summary>
/// 弹幕位置文本转换.
/// </summary>
internal sealed partial class DanmakuLocationConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var result = string.Empty;
        switch ((DanmakuLocation)value)
        {
            case DanmakuLocation.Scroll:
                result = ResourceToolkit.GetLocalizedString(StringNames.ScrollDanmaku);
                break;
            case DanmakuLocation.Top:
                result = ResourceToolkit.GetLocalizedString(StringNames.TopDanmaku);
                break;
            case DanmakuLocation.Bottom:
                result = ResourceToolkit.GetLocalizedString(StringNames.BottomDanmaku);
                break;
        }

        return result;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
