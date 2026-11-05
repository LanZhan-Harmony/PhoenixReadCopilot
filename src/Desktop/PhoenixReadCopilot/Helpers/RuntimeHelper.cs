using System.Runtime.InteropServices;

namespace PhoenixReadCopilot.Helpers;

/// <summary>
/// 运行环境辅助类.
/// </summary>
internal static partial class RuntimeHelper
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial int GetCurrentPackageFullName(
        ref int packageFullNameLength,
        nint packageFullName
    );

    /// <summary>
    /// 是否运行在 MSIX 打包环境下.
    /// </summary>
    public static bool IsMSIX
    {
        get
        {
            var length = 0;
            return GetCurrentPackageFullName(ref length, nint.Zero) != 15700; // APPMODEL_ERROR_NO_PACKAGE = 15700
        }
    }
}
