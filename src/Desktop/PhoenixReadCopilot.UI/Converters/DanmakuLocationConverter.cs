using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.UI.Strings;
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
                result = Resources.ScrollDanmaku;
                break;
            case DanmakuLocation.Top:
                result = Resources.TopDanmaku;
                break;
            case DanmakuLocation.Bottom:
                result = Resources.BottomDanmaku;
                break;
        }

        return result;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
