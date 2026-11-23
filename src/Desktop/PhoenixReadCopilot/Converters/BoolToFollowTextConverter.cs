using Microsoft.UI.Xaml.Data;
using PhoenixReadCopilot.Strings;

namespace PhoenixReadCopilot.Converters;

/// <summary>
/// 关注布尔值转为提示文本（已关注 / 关注）.
/// </summary>
internal sealed partial class BoolToFollowTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Resources.Followed : Resources.Follow;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
