using FluentIcons.Common;

namespace PhoenixReadCopilot.Converters;

internal static partial class Converter
{
    /// <summary>
    /// 图标变体转换.
    /// </summary>
    public static IconVariant BoolToIconVariant(bool isFilled) =>
        isFilled ? IconVariant.Filled : IconVariant.Regular;
}
