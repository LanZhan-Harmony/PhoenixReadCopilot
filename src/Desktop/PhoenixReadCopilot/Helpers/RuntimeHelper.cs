using System.Reflection;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;

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
            int length = 0;
            return GetCurrentPackageFullName(ref length, nint.Zero) != 15700; // APPMODEL_ERROR_NO_PACKAGE = 15700
        }
    }

    public static string AppVersion
    {
        get
        {
            if (IsMSIX)
            {
                PackageVersion appVersion = Package.Current.Id.Version;
                return $"{appVersion.Major}.{appVersion.Minor}.{appVersion.Build}.{appVersion.Revision}";
            }

            Version? version = Assembly.GetEntryAssembly()?.GetName().Version
                ?? Assembly.GetExecutingAssembly().GetName().Version;
            return $"{version?.Major}.{version?.Minor}.{version?.Build}.{version?.Revision}";
        }
    }
}
