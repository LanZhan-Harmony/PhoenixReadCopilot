namespace WinUIKernel.Share.Extensions;

internal static class LayoutExtensions
{
    public static bool IsValidSizeDimension(this double value) => value >= 0 && !double.IsInfinity(value);
}
