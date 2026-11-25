using Microsoft.UI.Xaml;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 非空字符串转换为可见性.
    /// </summary>
    public static Visibility StringToVisibility(string? value) =>
        string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>
    /// 数量大于 0 时转换为可见性.
    /// </summary>
    public static Visibility IntCountToVisibility(int count) =>
        count > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// 数量大于 0 时转换为可见性.
    /// </summary>
    public static Visibility LongCountToVisibility(long count) =>
        count > 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// 非空对象转换为可见性.
    /// </summary>
    public static Visibility ObjectToVisibility(object? value) =>
        value is not null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// 对象为空时转换为可见性.
    /// </summary>
    public static Visibility InvertObjectToVisibility(object? value) =>
        value is null ? Visibility.Visible : Visibility.Collapsed;
}
