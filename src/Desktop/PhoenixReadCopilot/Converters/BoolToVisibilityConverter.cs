using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace PhoenixReadCopilot.Converters;

/// <summary>
/// <see cref="bool"/> to <see cref="Visibility"/>.
/// </summary>
internal sealed partial class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Whether to invert the value.
    /// </summary>
    public bool IsReverse { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Visibility vis = Visibility.Collapsed;
        if (value is bool v)
        {
            vis = IsReverse
                ? v
                    ? Visibility.Collapsed
                    : Visibility.Visible
                : v
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        return vis;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
